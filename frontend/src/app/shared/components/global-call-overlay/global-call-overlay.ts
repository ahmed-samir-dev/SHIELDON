import { Component, inject, OnDestroy, AfterViewChecked, signal, effect, ElementRef, ViewChild, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatService } from '../../../core/services/chat.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-global-call-overlay',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './global-call-overlay.html',
  styleUrls: ['./global-call-overlay.scss']
})
export class GlobalCallOverlayComponent implements OnDestroy, AfterViewChecked {
  chatService = inject(ChatService);
  private toastr = inject(ToastrService);

  // ── WebRTC State ──────────────────────────────────────────────────────────
  localStream = signal<MediaStream | null>(null);
  remoteStream = signal<MediaStream | null>(null);
  isInCall = signal<boolean>(false);
  isIncomingCall = signal<boolean>(false);
  incomingCallerId = signal<string | null>(null);
  incomingCallerName = signal<string | null>(null);
  callTargetName = signal<string | null>(null);
  private incomingSignal: string | null = null;
  private peerConnection: RTCPeerConnection | null = null;
  private currentCallTargetId: string | null = null;

  // Audio state for synthesized ringtone
  private audioCtx?: AudioContext;
  private ringInterval?: any;

  @ViewChild('localVideo') localVideoRef?: ElementRef<HTMLVideoElement>;
  @ViewChild('remoteVideo') remoteVideoRef?: ElementRef<HTMLVideoElement>;

  constructor() {
    effect(() => {
      const targetId = this.chatService.outgoingCallTarget();
      if (targetId && !this.isInCall()) {
        this.initiateCall(targetId);
      }
    }, { allowSignalWrites: true });

    effect(() => {
      const call = this.chatService.incomingCall();
      if (!call) {
        if (untracked(() => this.isIncomingCall())) {
            this.stopRingtone();
            untracked(() => this.isIncomingCall.set(false));
        }
        return;
      }
      this.incomingCallerId.set(call.callerId);
      this.incomingSignal = call.signal;
      this.isIncomingCall.set(true);
      
      const conv = untracked(() => this.chatService.inbox()).find(c => c.otherUserId === call.callerId);
      this.incomingCallerName.set(conv?.otherUserName ?? 'Unknown');
      
      this.startRingtone();
    }, { allowSignalWrites: true });

    effect(() => {
      const answer = this.chatService.callAnswered();
      if (!answer || !this.peerConnection) return;
      const desc = JSON.parse(answer.signal) as RTCSessionDescriptionInit;
      this.peerConnection.setRemoteDescription(new RTCSessionDescription(desc))
        .catch(err => console.error('Error setting remote description', err));
    }, { allowSignalWrites: true });

    effect(() => {
      const iceData = this.chatService.iceCandidateReceived();
      if (!iceData || !this.peerConnection) return;
      const candidate = JSON.parse(iceData.signal) as RTCIceCandidateInit;
      this.peerConnection.addIceCandidate(new RTCIceCandidate(candidate))
        .catch(err => console.error('Error adding ICE candidate', err));
    }, { allowSignalWrites: true });

    effect(() => {
      const endedBy = this.chatService.callEnded();
      if (!endedBy) {
          // Check if we need to teardown due to logout (which sets endedBy to null)
          // Use untracked to prevent this effect from firing when these signals change!
          const inCall = untracked(() => this.isInCall());
          const incoming = untracked(() => this.isIncomingCall());
          if (inCall || incoming) {
             this.teardownCall(false);
          }
          return;
      }

      const inCall = untracked(() => this.isInCall());
      const incoming = untracked(() => this.isIncomingCall());

      if (inCall && !untracked(() => this.remoteStream())) {
        this.toastr.warning('The user declined your call.', 'Call Declined');
      } else if (inCall || incoming) {
        this.toastr.info('The call was ended.', 'Call Ended');
      }
      this.teardownCall(false);
    }, { allowSignalWrites: true });
  }

  ngAfterViewChecked(): void {
    const local = this.localStream();
    if (this.localVideoRef?.nativeElement && this.localVideoRef.nativeElement.srcObject !== local) {
      this.localVideoRef.nativeElement.srcObject = local;
    }
    const remote = this.remoteStream();
    if (this.remoteVideoRef?.nativeElement && this.remoteVideoRef.nativeElement.srcObject !== remote) {
      this.remoteVideoRef.nativeElement.srcObject = remote;
    }
  }

  ngOnDestroy(): void {
    this.teardownCall(true);
  }

  async initiateCall(targetUserId: string): Promise<void> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
      this.localStream.set(stream);

      this.currentCallTargetId = targetUserId;
      
      const conv = this.chatService.inbox().find(c => c.otherUserId === targetUserId);
      this.callTargetName.set(conv?.otherUserName ?? 'Unknown');

      this.peerConnection = this.createPeerConnection(targetUserId);
      stream.getTracks().forEach(track => this.peerConnection!.addTrack(track, stream));

      const offer = await this.peerConnection.createOffer();
      await this.peerConnection.setLocalDescription(offer);

      await this.chatService.sendCallOffer({
        targetUserId: targetUserId,
        signal: JSON.stringify(offer)
      });

      this.isInCall.set(true);
    } catch (err) {
      console.error('Failed to initiate call', err);
      this.teardownCall(false);
    }
  }

  async acceptCall(): Promise<void> {
    if (!this.incomingCallerId() || !this.incomingSignal) return;
    const callerId = this.incomingCallerId()!;
    
    try {
      this.stopRingtone();
      const stream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
      this.localStream.set(stream);

      this.currentCallTargetId = callerId;
      this.callTargetName.set(this.incomingCallerName());
      this.peerConnection = this.createPeerConnection(callerId);

      stream.getTracks().forEach(track => this.peerConnection!.addTrack(track, stream));

      const offer = JSON.parse(this.incomingSignal) as RTCSessionDescriptionInit;
      await this.peerConnection.setRemoteDescription(new RTCSessionDescription(offer));

      const answer = await this.peerConnection.createAnswer();
      await this.peerConnection.setLocalDescription(answer);

      await this.chatService.sendCallAnswer({
        targetUserId: callerId,
        signal: JSON.stringify(answer)
      });

      this.isIncomingCall.set(false);
      this.isInCall.set(true);
    } catch (err) {
      console.error('Failed to accept call', err);
      this.teardownCall(false);
    }
  }

  rejectCall(): void {
    this.stopRingtone();
    const callerId = this.incomingCallerId();
    if (callerId) {
      this.chatService.endCall(callerId);
    }
    this.isIncomingCall.set(false);
    this.incomingCallerId.set(null);
    this.incomingCallerName.set(null);
    this.incomingSignal = null;
  }

  endCall(): void {
    this.teardownCall(true);
  }

  private teardownCall(notifyRemote: boolean): void {
    if (notifyRemote && this.currentCallTargetId) {
      this.chatService.endCall(this.currentCallTargetId);
    }
    
    this.stopRingtone();

    const stream = this.localStream();
    if (stream) stream.getTracks().forEach(t => t.stop());

    const rStream = this.remoteStream();
    if (rStream) rStream.getTracks().forEach(t => t.stop());

    if (this.peerConnection) {
      this.peerConnection.close();
      this.peerConnection = null;
    }

    this.localStream.set(null);
    this.remoteStream.set(null);
    this.isInCall.set(false);
    this.isIncomingCall.set(false);
    this.incomingCallerId.set(null);
    this.incomingCallerName.set(null);
    this.callTargetName.set(null);
    this.incomingSignal = null;
    this.currentCallTargetId = null;
    
    // Clear the global state in the service so old signals don't resurrect calls
    this.chatService.outgoingCallTarget.set(null);
    this.chatService.incomingCall.set(null);
    this.chatService.callAnswered.set(null);
    this.chatService.callEnded.set(null);
  }

  private createPeerConnection(targetUserId: string): RTCPeerConnection {
    const pc = new RTCPeerConnection({
      iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
    });

    pc.onicecandidate = (event) => {
      if (event.candidate) {
        this.chatService.sendIceCandidate({
          targetUserId,
          signal: JSON.stringify(event.candidate)
        });
      }
    };

    pc.ontrack = (event) => {
      if (event.streams && event.streams[0]) {
        this.remoteStream.set(event.streams[0]);
      }
    };

    pc.onconnectionstatechange = () => {
      if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed') {
        this.teardownCall(false);
      }
    };

    return pc;
  }

  // ── Ringtone Synthesizer ──────────────────────────────────────────────────
  private startRingtone() {
    if (!this.audioCtx) {
      this.audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
    }
    
    const playRing = () => {
      if (this.audioCtx?.state === 'suspended') {
          this.audioCtx.resume();
      }
      const osc1 = this.audioCtx!.createOscillator();
      const osc2 = this.audioCtx!.createOscillator();
      const gainNode = this.audioCtx!.createGain();

      osc1.type = 'sine';
      osc1.frequency.value = 440;
      osc2.type = 'sine';
      osc2.frequency.value = 480;

      const lfo = this.audioCtx!.createOscillator();
      lfo.type = 'sine';
      lfo.frequency.value = 20;

      const lfoGain = this.audioCtx!.createGain();
      lfoGain.gain.value = 0.5;

      lfo.connect(lfoGain);
      lfoGain.connect(gainNode.gain);

      osc1.connect(gainNode);
      osc2.connect(gainNode);
      gainNode.connect(this.audioCtx!.destination);

      const now = this.audioCtx!.currentTime;
      gainNode.gain.setValueAtTime(0, now);
      gainNode.gain.linearRampToValueAtTime(0.5, now + 0.1);
      gainNode.gain.setValueAtTime(0.5, now + 1.9);
      gainNode.gain.linearRampToValueAtTime(0, now + 2.0);

      osc1.start(now);
      osc2.start(now);
      lfo.start(now);
      osc1.stop(now + 2.0);
      osc2.stop(now + 2.0);
      lfo.stop(now + 2.0);
    };

    playRing();
    this.ringInterval = setInterval(playRing, 4000);
  }

  private stopRingtone() {
    if (this.ringInterval) {
      clearInterval(this.ringInterval);
      this.ringInterval = null;
    }
  }
}

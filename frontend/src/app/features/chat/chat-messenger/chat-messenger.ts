import { Component, inject, OnInit, OnDestroy, AfterViewChecked, signal, computed, effect, ElementRef, ViewChild, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { ChatMessageDto, ChatUserDto, ConversationSummaryDto, SendMessageRequest, SendGroupMessageRequest, AttachmentType, GroupParticipantDto, RenameGroupRequest, AddGroupMembersRequest } from '../../../core/models/chat.model';
import { environment } from '../../../../environments/environment';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ToastrService } from 'ngx-toastr';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-chat-messenger',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './chat-messenger.html',
  styleUrls: ['./chat-messenger.scss']
})
export class ChatMessengerComponent implements OnInit, OnDestroy, AfterViewChecked {
  chatService = inject(ChatService);
  authService = inject(AuthService);
  private toastr = inject(ToastrService);
  translate = inject(TranslateService);

  // Component State
  activeConversation = signal<ConversationSummaryDto | null>(null);
  newMessageContent = signal<string>('');
  
  // New Chat Modal State
  isNewChatModalOpen = signal<boolean>(false);
  availableUsers = signal<ChatUserDto[]>([]);
  userSearchQuery = signal<string>('');
  selectedRoleFilter = signal<string>('All');
  selectedStatusFilter = signal<string>('All');
  
  // New Inbox Filter
  inboxFilter = signal<'All' | 'Online' | 'Offline'>('All');
  inboxRoleFilter = signal<string>('All');
  
  // Message UI State
  expandedMessages = signal<Set<string>>(new Set());
  private typingTimeout: any = null;

  // Voice Note State
  isRecording = signal<boolean>(false);
  recordingTime = signal<number>(0);
  private mediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];
  private recordingInterval: any;

  // Group Creation State
  isGroupCreation = signal<boolean>(false);
  newGroupName = signal<string>('');
  selectedMemberIds = signal<Set<string>>(new Set());

  // Group Management State
  isManagingGroup = signal<boolean>(false);
  manageGroupParticipants = signal<GroupParticipantDto[]>([]);
  manageGroupName = signal<string>('');
  manageUserSearchQuery = signal<string>('');
  manageSelectedMemberIds = signal<Set<string>>(new Set());

  @ViewChild('messagesArea') private messagesArea?: ElementRef;

  // ── WebRTC State ──────────────────────────────────────────────────────────
  // WebRTC logic has been moved to GlobalCallOverlayComponent

  constructor() {
    effect(() => {
      // Re-run whenever active conversation messages update
      const msgs = this.chatService.activeConversationMessages();
      // Wait for Angular to update the DOM before scrolling
      setTimeout(() => this.scrollToBottom(), 50);
    });

    effect(() => {
      const renameData = this.chatService.groupRenamed();
      if (renameData) {
        const active = untracked(() => this.activeConversation());
        if (active && active.conversationId === renameData.conversationId) {
          this.activeConversation.set({
            ...active,
            groupName: renameData.newGroupName
          });
        }
      }
    });
  }

  ngOnInit(): void {
    this.chatService.startConnection();
  }

  ngAfterViewChecked(): void {}

  ngOnDestroy(): void {}

  // ── Conversation Helpers ──────────────────────────────────────────────────

  openConversation(conv: ConversationSummaryDto): void {
    this.activeConversation.set(conv);
    this.chatService.loadMessages(conv.conversationId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          // Pass conversationId so the service calls markAsRead automatically
          this.chatService.setActiveConversationMessages(res.data, conv.conversationId);
        }
      }
    });
  }

  // ── Messaging ─────────────────────────────────────────────────────────────

  async sendMessage(): Promise<void> {
    const content = this.newMessageContent().trim();
    if (!content) return;

    const conv = this.activeConversation();
    if (!conv) return;

    if (conv.isGroup) {
      const request: SendGroupMessageRequest = {
        conversationId: conv.conversationId,
        content
      };
      await this.chatService.sendGroupMessage(request);
    } else {
      const recipientId = conv.otherUserId;
      if (!recipientId) return;

      const request: SendMessageRequest = {
        recipientId,
        content
      };
      await this.chatService.sendMessage(request);
    }

    this.newMessageContent.set('');
  }

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  // ── New Chat Modal ────────────────────────────────────────────────────────

  openNewChatModal(): void {
    this.isNewChatModalOpen.set(true);
    this.chatService.getChatUsers().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.availableUsers.set(res.data);
        }
      }
    });
  }

  closeNewChatModal(): void {
    this.isNewChatModalOpen.set(false);
    this.userSearchQuery.set('');
    this.isGroupCreation.set(false);
    this.newGroupName.set('');
    this.selectedMemberIds.set(new Set());
  }

  toggleGroupCreation(): void {
    this.isGroupCreation.set(!this.isGroupCreation());
    this.selectedMemberIds.set(new Set());
  }

  toggleMemberSelection(userId: string): void {
    this.selectedMemberIds.update(set => {
      const newSet = new Set(set);
      if (newSet.has(userId)) newSet.delete(userId);
      else newSet.add(userId);
      return newSet;
    });
  }

  createGroup(): void {
    const name = this.newGroupName().trim();
    const members = Array.from(this.selectedMemberIds());
    if (!name || members.length === 0) return;
    this.chatService.createGroup({ groupName: name, memberIds: members }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.closeNewChatModal();
          this.chatService.refreshInbox();
          this.openConversation(res.data);
        }
      }
    });
  }

  startChatWithUser(user: ChatUserDto): void {
    this.closeNewChatModal();
    this.chatService.getConversationId(user.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          const convId = res.data;
          const inbox = this.chatService.inbox();
          const existing = inbox.find(c => c.conversationId === convId);
          if (existing) {
            this.openConversation(existing);
          } else {
            this.chatService.refreshInbox();
            this.openConversation({
              conversationId: convId,
              otherUserId: user.id,
              otherUserName: user.fullName,
              otherUserAvatarUrl: user.avatarUrl,
              otherUserRole: user.role,
              isGroup: false,
              lastMessagePreview: '',
              lastMessageAt: new Date().toISOString(),
              unreadCount: 0
            });
          }
        } else {
          this.activeConversation.set({
            conversationId: 'NEW',
            otherUserId: user.id,
            otherUserName: user.fullName,
            otherUserAvatarUrl: user.avatarUrl,
            otherUserRole: user.role,
            isGroup: false,
            lastMessagePreview: '',
            lastMessageAt: new Date().toISOString(),
            unreadCount: 0
          });
          this.chatService.setActiveConversationMessages([]);
        }
      }
    });
  }

  // ── Filters ───────────────────────────────────────────────────────────────

  filteredUsers = computed<ChatUserDto[]>(() => {
    const q = this.userSearchQuery().toLowerCase();
    const role = this.selectedRoleFilter();
    const status = this.selectedStatusFilter();
    const onlineList = this.chatService.onlineUsers();
    
    return this.availableUsers().filter(u => {
      const matchesSearch = u.fullName.toLowerCase().includes(q);
      const matchesRole = role === 'All' || u.role === role;
      const isOnline = onlineList.includes(u.id);
      const matchesStatus = status === 'All' || (status === 'Online' && isOnline) || (status === 'Offline' && !isOnline);
      return matchesSearch && matchesRole && matchesStatus;
    });
  });

  filteredInbox = computed<ConversationSummaryDto[]>(() => {
    const statusFilter = this.inboxFilter();
    const roleFilter = this.inboxRoleFilter();
    const onlineList = this.chatService.onlineUsers();
    
    return this.chatService.inbox().filter(conv => {
      let matchesStatus = true;
      if (statusFilter === 'Online') matchesStatus = conv.otherUserId ? onlineList.includes(conv.otherUserId) : false;
      if (statusFilter === 'Offline') matchesStatus = !conv.otherUserId || !onlineList.includes(conv.otherUserId);
      
      let matchesRole = true;
      if (roleFilter !== 'All') {
        if (roleFilter === 'Group') {
          matchesRole = conv.isGroup;
        } else {
          matchesRole = !conv.isGroup && conv.otherUserRole === roleFilter;
        }
      }
      
      return matchesStatus && matchesRole;
    });
  });

  // ── Utilities ─────────────────────────────────────────────────────────────

  getAvatarUrl(path: string | undefined | null): string | null {
    if (!path) return null;
    if (path.startsWith('http')) return path;
    const apiUrl = environment.apiUrl.replace('/api', '');
    return `${apiUrl}${path.startsWith('/') ? '' : '/'}${path}`;
  }

  getAttachmentLabel(url: string | null | undefined): string {
    if (!url) return 'Document';
    const ext = url.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return 'PDF Document';
    if (ext === 'docx' || ext === 'doc') return 'Word Document';
    if (ext === 'pptx' || ext === 'ppt') return 'PowerPoint Slides';
    return 'Document';
  }

  getAttachmentName(url: string | null | undefined): string {
    if (!url) return 'document.pdf';
    const parts = url.split('/');
    const fullName = parts[parts.length - 1]; // e.g. "my-file_guid.pdf" or "guid.pdf"
    
    const lastUnderscoreIndex = fullName.lastIndexOf('_');
    const dotIndex = fullName.lastIndexOf('.');
    
    if (lastUnderscoreIndex > -1 && dotIndex > lastUnderscoreIndex) {
      const originalName = fullName.substring(0, lastUnderscoreIndex);
      const ext = fullName.substring(dotIndex);
      return originalName + ext; // "my-file.pdf"
    }
    
    // For legacy/GUID files, return a friendly generic name matching the extension
    if (dotIndex > -1) {
      const ext = fullName.substring(dotIndex).toLowerCase();
      if (ext === '.pdf') return 'document.pdf';
      if (ext === '.docx' || ext === '.doc') return 'document.docx';
      if (ext === '.pptx' || ext === '.ppt') return 'presentation.pptx';
      return 'attachment' + ext;
    }
    
    return 'document.pdf';
  }

  getAttachmentType(url: string | null | undefined): string {
    if (!url) return 'generic';
    const ext = url.split('.').pop()?.toLowerCase();
    if (ext === 'pdf') return 'pdf';
    if (ext === 'docx' || ext === 'doc') return 'word';
    if (ext === 'pptx' || ext === 'ppt') return 'powerpoint';
    return 'generic';
  }

  onTyping(): void {
    const conv = this.activeConversation();
    if (!conv || conv.conversationId === 'NEW') return;

    if (!this.typingTimeout) {
      this.chatService.notifyTyping(conv.otherUserId || '');
      this.typingTimeout = setTimeout(() => {
        this.typingTimeout = null;
      }, 1000);
    }
  }

  toggleMessageSize(messageId: string): void {
    this.expandedMessages.update(set => {
      const newSet = new Set(set);
      if (newSet.has(messageId)) {
        newSet.delete(messageId);
      } else {
        newSet.add(messageId);
      }
      return newSet;
    });
  }

  private scrollToBottom(): void {
    if (this.messagesArea) {
      try {
        this.messagesArea.nativeElement.scrollTop = this.messagesArea.nativeElement.scrollHeight;
      } catch(err) {}
    }
  }

  // ── Attachment Helper ─────────────────────────────────────────────────────

  private async sendAttachmentForConversation(
    conv: ConversationSummaryDto,
    attachmentUrl: string,
    attachmentType: AttachmentType
  ): Promise<void> {
    if (conv.isGroup) {
      await this.chatService.sendGroupMessage({
        conversationId: conv.conversationId,
        content: '',
        attachmentUrl,
        attachmentType
      });
    } else if (conv.otherUserId) {
      await this.chatService.sendAttachmentMessage(
        conv.otherUserId,
        '',
        attachmentUrl,
        attachmentType
      );
    }
  }

  // ── Voice Notes ───────────────────────────────────────────────────────────

  async startRecording() {
    const conv = this.activeConversation();
    if (!conv) return;
    // Must have either a DM recipient or be a group chat
    if (!conv.isGroup && !conv.otherUserId) return;
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.mediaRecorder = new MediaRecorder(stream);
      this.audioChunks = [];
      this.mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) this.audioChunks.push(e.data);
      };
      this.mediaRecorder.onstop = () => {
        const audioBlob = new Blob(this.audioChunks, { type: 'audio/webm' });
        const currentConv = this.activeConversation();
        if (audioBlob.size > 0 && currentConv) {
          const file = new File([audioBlob], 'voicenote.webm', { type: 'audio/webm' });
          this.chatService.uploadAttachment(file).subscribe({
            next: async (res) => {
              if (res.success && res.data) {
                await this.sendAttachmentForConversation(currentConv, res.data.url, res.data.attachmentType);
              }
            },
            error: (err) => {
              console.error('Voice note upload failed', err);
              const errMsg = err?.error?.message || 'Failed to upload voice note.';
              this.toastr.error(errMsg, 'Upload Error');
            }
          });
        }
        stream.getTracks().forEach(t => t.stop());
      };
      
      this.mediaRecorder.start();
      this.isRecording.set(true);
      this.recordingTime.set(0);
      
      this.recordingInterval = setInterval(() => {
        this.recordingTime.update(t => {
          if (t >= 300) { // 5 minutes max
            this.stopRecording();
            return t;
          }
          return t + 1;
        });
      }, 1000);
      
    } catch (err) {
      console.error('Error accessing microphone', err);
    }
  }

  stopRecording(cancel = false) {
    if (this.mediaRecorder && this.isRecording()) {
      if (cancel) {
        this.audioChunks = [];
        // Override onstop so it does nothing
        this.mediaRecorder.onstop = () => {};
      }
      this.mediaRecorder.stop();
      this.isRecording.set(false);
      clearInterval(this.recordingInterval);
    }
  }

  get formatRecordingTime(): string {
    const mins = Math.floor(this.recordingTime() / 60);
    const secs = this.recordingTime() % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  triggerAttachmentUpload(input: HTMLInputElement) {
    input.click();
  }

  onFileSelected(event: any) {
    const file: File = event.target.files[0];
    if (!file) return;

    // Client-side file format check
    const allowedExtensions = ['.pdf', '.docx', '.pptx', '.jpg', '.jpeg', '.png', '.mp3', '.wav', '.webm', '.ogg', '.m4a'];
    const fileName = file.name.toLowerCase();
    const hasValidExtension = allowedExtensions.some(ext => fileName.endsWith(ext));
    if (!hasValidExtension) {
      this.toastr.error('Invalid file format. Allowed formats: PDF, DOCX, PPTX, JPG, JPEG, PNG, MP3, WAV, WEBM, OGG, M4A.', 'Upload Error');
      event.target.value = '';
      return;
    }

    // Client-side file size check (10MB limit)
    const maxSizeBytes = 10 * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      this.toastr.error('File size exceeds the 10MB limit.', 'Upload Error');
      event.target.value = '';
      return;
    }

    const conv = this.activeConversation();
    if (!conv) return;
    if (!conv.isGroup && !conv.otherUserId) return;

    this.chatService.uploadAttachment(file).subscribe({
      next: async (res) => {
        if (res.success && res.data) {
          await this.sendAttachmentForConversation(conv, res.data.url, res.data.attachmentType);
        }
      },
      error: (err) => {
        console.error('Attachment upload failed', err);
        const errMsg = err?.error?.message || 'Failed to upload attachment.';
        this.toastr.error(errMsg, 'Upload Error');
      }
    });

    // Reset the input so the same file can be re-selected if needed
    event.target.value = '';
  }

  // ── WebRTC: 1-on-1 Video Calls ────────────────────────────────────────────

  async initiateCall(): Promise<void> {
    const conv = this.activeConversation();
    if (!conv || conv.isGroup || !conv.otherUserId) return;
    
    // Trigger global call overlay
    this.chatService.startOutgoingCall(conv.otherUserId);
  }

  // ── Group Management ──────────────────────────────────────────────────────

  openManageGroupModal(): void {
    const conv = this.activeConversation();
    if (!conv || !conv.isGroup) return;

    this.manageGroupName.set(conv.groupName || '');
    this.isManagingGroup.set(true);
    
    // Load participants
    this.chatService.getGroupParticipants(conv.conversationId).subscribe({
      next: (participants) => {
        this.manageGroupParticipants.set(participants);
      }
    });

    // Pre-load available users so we can add new ones
    if (this.availableUsers().length === 0) {
      this.chatService.getChatUsers().subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.availableUsers.set(res.data);
          }
        }
      });
    }
  }

  closeManageGroupModal(): void {
    this.isManagingGroup.set(false);
    this.manageGroupParticipants.set([]);
    this.manageUserSearchQuery.set('');
    this.manageSelectedMemberIds.set(new Set());
  }

  filteredUsersForGroupManage = computed<ChatUserDto[]>(() => {
    const query = this.manageUserSearchQuery().toLowerCase();
    const existingMemberIds = new Set(this.manageGroupParticipants().map(p => p.userId));
    return this.availableUsers().filter(user => 
      !existingMemberIds.has(user.id) &&
      (user.fullName.toLowerCase().includes(query) || user.role.toLowerCase().includes(query))
    );
  });

  renameGroup(): void {
    const conv = this.activeConversation();
    const newName = this.manageGroupName().trim();
    if (!conv || !newName || newName === conv.groupName) return;

    this.chatService.renameGroup(conv.conversationId, { newGroupName: newName }).subscribe({
      next: (res) => {
        this.toastr.success('Group renamed successfully');
        this.closeManageGroupModal();
      },
      error: (err) => {
        this.toastr.error('Failed to rename group');
      }
    });
  }

  toggleManageMemberSelection(userId: string): void {
    this.manageSelectedMemberIds.update(set => {
      const newSet = new Set(set);
      if (newSet.has(userId)) newSet.delete(userId);
      else newSet.add(userId);
      return newSet;
    });
  }

  addGroupMembers(): void {
    const conv = this.activeConversation();
    const membersToAdd = Array.from(this.manageSelectedMemberIds());
    if (!conv || membersToAdd.length === 0) return;

    this.chatService.addGroupMembers(conv.conversationId, { memberIds: membersToAdd }).subscribe({
      next: () => {
        this.toastr.success('Members added successfully');
        this.manageSelectedMemberIds.set(new Set());
        // Reload participants
        this.chatService.getGroupParticipants(conv.conversationId).subscribe({
          next: (p) => this.manageGroupParticipants.set(p)
        });
      },
      error: () => this.toastr.error('Failed to add members')
    });
  }

  isCurrentUserGroupAdmin(): boolean {
    const currentUserId = this.authService.currentUser()?.userId;
    if (!currentUserId) return false;
    return this.manageGroupParticipants().some(p => p.userId === currentUserId && p.isAdmin);
  }

  removeGroupMember(userId: string): void {
    const conv = this.activeConversation();
    if (!conv) return;

    Swal.fire({
      title: this.translate.instant('Are you sure?'),
      text: this.translate.instant('Are you sure you want to remove this member?'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#64748b',
      confirmButtonText: this.translate.instant('Yes, remove them')
    }).then((result) => {
      if (result.isConfirmed) {
        this.chatService.removeGroupMember(conv.conversationId, userId).subscribe({
          next: () => {
            this.toastr.success('Member removed');
            this.manageGroupParticipants.update(p => p.filter(u => u.userId !== userId));
          },
          error: () => this.toastr.error('Failed to remove member')
        });
      }
    });
  }

  deleteGroup(): void {
    const conv = this.activeConversation();
    if (!conv) return;

    Swal.fire({
      title: this.translate.instant('Are you sure?'),
      text: this.translate.instant('Are you sure you want to permanently delete this group? This action cannot be undone.'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444',
      cancelButtonColor: '#64748b',
      confirmButtonText: this.translate.instant('Yes, delete group')
    }).then((result) => {
      if (result.isConfirmed) {
        this.chatService.deleteGroup(conv.conversationId).subscribe({
          next: () => {
            this.toastr.success('Group deleted');
            this.closeManageGroupModal();
            this.activeConversation.set(null);
          },
          error: () => this.toastr.error('Failed to delete group')
        });
      }
    });
  }

}

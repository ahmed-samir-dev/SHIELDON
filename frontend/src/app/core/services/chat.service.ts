import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { Observable, map } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { 
  ChatMessageDto, 
  ChatUserDto, 
  ConversationSummaryDto, 
  SendMessageRequest,
  SendGroupMessageRequest,
  CreateGroupRequest,
  RenameGroupRequest,
  AddGroupMembersRequest,
  AttachmentUploadResponse,
  MessageReactionDto,
  ReactToMessageRequest,
  ForwardMessageRequest,
  GroupParticipantDto,
  WebRtcSignalDto,
  MessageStatus,
  AttachmentType
} from '../models/chat.model';
import { ToastrService } from 'ngx-toastr';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);
  private translate = inject(TranslateService);

  private hubConnection: HubConnection | null = null;
  
  // ── Reactive State (Angular Signals) ──────────────────────────────────────
  readonly inbox = signal<ConversationSummaryDto[]>([]);
  private _activeConversationMessages = signal<ChatMessageDto[]>([]);
  private _unreadTotalCount = signal<number>(0);
  private _onlineUsers = signal<string[]>([]);
  readonly activeConversationMessages = this._activeConversationMessages.asReadonly();
  readonly unreadTotalCount = this._unreadTotalCount.asReadonly();
  readonly onlineUsers = this._onlineUsers.asReadonly();
  readonly isConnected = signal<boolean>(false);
  
  // Array of active typing indicators (conversationId, userId, userName)
  readonly typingIndicators = signal<{conversationId: string, userId: string, userName: string}[]>([]);
  private typingTimeouts = new Map<string, any>();

  // WebRTC Signals - set by incoming SignalR events, consumed by the component via effect()
  readonly incomingCall = signal<{callerId: string, signal: string} | null>(null);
  readonly callAnswered = signal<{answererId: string, signal: string} | null>(null);
  readonly iceCandidateReceived = signal<{senderId: string, signal: string} | null>(null);
  readonly callEnded = signal<string | null>(null);
  readonly outgoingCallTarget = signal<string | null>(null);

  readonly messageStatusChanged = signal<{conversationId: string, status: MessageStatus, updatedByUserId: string} | null>(null);
  readonly groupRenamed = signal<{conversationId: string, newGroupName: string} | null>(null);

  // Expose an observable-like signal for user offline events with last seen updates
  readonly userOfflineState = signal<{userId: string, lastSeenAt: string} | null>(null);

  // ── REST Methods ─────────────────────────────────────────────────────────

  loadInbox(): Observable<ApiResponse<ConversationSummaryDto[]>> {
    return this.http.get<ApiResponse<ConversationSummaryDto[]>>(`${environment.apiUrl}/chat/inbox`);
  }

  loadMessages(conversationId: string): Observable<ApiResponse<ChatMessageDto[]>> {
    return this.http.get<ApiResponse<ChatMessageDto[]>>(`${environment.apiUrl}/chat/conversations/${conversationId}/messages`);
  }

  getChatUsers(): Observable<ApiResponse<ChatUserDto[]>> {
    return this.http.get<ApiResponse<ChatUserDto[]>>(`${environment.apiUrl}/chat/users`);
  }

  getConversationId(recipientId: string): Observable<ApiResponse<string | null>> {
    return this.http.get<ApiResponse<string | null>>(`${environment.apiUrl}/chat/conversation-id?recipientId=${recipientId}`);
  }

  createGroup(request: CreateGroupRequest): Observable<ApiResponse<ConversationSummaryDto>> {
    return this.http.post<ApiResponse<ConversationSummaryDto>>(`${environment.apiUrl}/chat/group`, request);
  }

  getGroupParticipants(conversationId: string): Observable<GroupParticipantDto[]> {
    return this.http.get<ApiResponse<GroupParticipantDto[]>>(`${environment.apiUrl}/chat/group/${conversationId}/participants`)
      .pipe(map(response => response.data));
  }

  uploadAttachment(file: File): Observable<ApiResponse<AttachmentUploadResponse>> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ApiResponse<AttachmentUploadResponse>>(`${environment.apiUrl}/chat/upload`, formData);
  }

  renameGroup(conversationId: string, request: RenameGroupRequest): Observable<ConversationSummaryDto> {
    return this.http.put<ApiResponse<ConversationSummaryDto>>(`${environment.apiUrl}/chat/group/${conversationId}/rename`, request)
      .pipe(map(response => response.data));
  }

  addGroupMembers(conversationId: string, request: AddGroupMembersRequest): Observable<any> {
    return this.http.post<ApiResponse<any>>(`${environment.apiUrl}/chat/group/${conversationId}/members`, request)
      .pipe(map(response => response.data));
  }

  removeGroupMember(conversationId: string, userId: string): Observable<any> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/chat/group/${conversationId}/members/${userId}`)
      .pipe(map(response => response.data));
  }

  deleteGroup(conversationId: string): Observable<any> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/chat/group/${conversationId}`)
      .pipe(map(response => response.data));
  }

  reactToMessage(messageId: string, emoji: string): Observable<MessageReactionDto[]> {
    return this.http.post<ApiResponse<MessageReactionDto[]>>(`${environment.apiUrl}/chat/messages/${messageId}/react`, { emoji })
      .pipe(map(response => response.data));
  }

  deleteMessage(messageId: string): Observable<any> {
    return this.http.delete<ApiResponse<any>>(`${environment.apiUrl}/chat/messages/${messageId}`)
      .pipe(map(response => response.data));
  }

  forwardMessage(messageId: string, targetConversationIds: string[]): Observable<ChatMessageDto[]> {
    return this.http.post<ApiResponse<ChatMessageDto[]>>(`${environment.apiUrl}/chat/messages/forward`, { messageId, targetConversationIds })
      .pipe(map(response => response.data));
  }

  // ── SignalR Methods ──────────────────────────────────────────────────────

  private setupSignalREvents(): void {
    if (!this.hubConnection) return;

    this.hubConnection.on('ReceiveMessage', (message: ChatMessageDto) => {
      // Backend sets IsOwnMessage = true for the SENDER and sends it to both.
      // We must explicitly recalculate it for the recipient context.
      const currentUserId = this.authService.currentUser()?.userId;
      message.isOwnMessage = message.senderId === currentUserId;
      
      // Coerce string enums (if any) to numbers
      if (typeof message.status === 'string') {
        message.status = MessageStatus[message.status as keyof typeof MessageStatus];
      }
      if (typeof message.attachmentType === 'string') {
        message.attachmentType = AttachmentType[message.attachmentType as keyof typeof AttachmentType];
      }

      this.handleIncomingMessage(message);
    });

    this.hubConnection.on('UserIsOnline', (userId: string) => {
      this._onlineUsers.update(users => [...new Set([...users, userId])]);
    });

    this.hubConnection.on('UserIsOffline', (userId: string, lastSeenAt: string) => {
      this._onlineUsers.update(users => users.filter(id => id !== userId));
      this.userOfflineState.set({ userId, lastSeenAt });
    });

    this.hubConnection.on('UserIsTyping', (conversationId: string, userId: string, userName: string) => {
      this.typingIndicators.update(indicators => {
        const exists = indicators.find(i => i.conversationId === conversationId && i.userId === userId);
        if (exists) return indicators;
        return [...indicators, { conversationId, userId, userName }];
      });
      
      const timeoutKey = `${conversationId}_${userId}`;
      if (this.typingTimeouts.has(timeoutKey)) {
        clearTimeout(this.typingTimeouts.get(timeoutKey));
      }

      const timeout = setTimeout(() => {
        this.typingIndicators.update(indicators => indicators.filter(i => !(i.conversationId === conversationId && i.userId === userId)));
        this.typingTimeouts.delete(timeoutKey);
      }, 2000); // 2 second auto-clear
      
      this.typingTimeouts.set(timeoutKey, timeout);
    });

    // ── Delivery Receipts ─────────────────────────────────────────────────
    // The backend sends this to the RECIPIENT when a new message arrives.
    // The recipient should respond by calling MarkAsDelivered so the sender sees ✓✓.
    this.hubConnection.on('MessagesDelivered', (senderIdStr: string) => {
      // When we (the recipient) receive this notification, tell the server
      // to mark those Sent→Delivered and notify the original sender.
      const msgs = this._activeConversationMessages();
      if (msgs.length === 0) return;
      const conversationId = msgs[0].conversationId;
      this.markAsDelivered(senderIdStr, conversationId);
    });

    // The backend sends MessageStatusChanged to the SENDER when their messages
    // are marked Delivered or Read. Update the local message list in real-time.
    this.hubConnection.on('MessageStatusChanged', (data: {conversationId: string, status: number, updatedByUserId: string}) => {
      this.messageStatusChanged.set({
        conversationId: data.conversationId,
        status: data.status as MessageStatus,
        updatedByUserId: data.updatedByUserId
      });

      // Update every message in this conversation that is BELOW the new status.
      // Only upgrade status (Sent→Delivered→Read), never downgrade.
      this._activeConversationMessages.update(msgs => {
        if (msgs.length > 0 && msgs[0].conversationId === data.conversationId) {
          return msgs.map(m => {
            // Only update messages sent BY ME and that haven't reached this status yet
            if (m.isOwnMessage && m.status < data.status) {
              return { ...m, status: data.status as MessageStatus };
            }
            return m;
          });
        }
        return msgs;
      });
    });

    // ── WebRTC Signaling ──────────────────────────────────────────────────
    // The hub sends PascalCase keys; SignalR's default JSON serializer lowercases them.
    // We handle both cases below for safety.
    this.hubConnection.on('CallIncoming', (data: any) => {
      const callerId: string = data.callerId ?? data.CallerID ?? data.callerId;
      const signal: string  = data.signal  ?? data.Signal;
      this.incomingCall.set({ callerId, signal });
    });

    this.hubConnection.on('CallAnswered', (data: any) => {
      const answererId: string = data.answererId ?? data.AnswererId;
      const signal: string    = data.signal     ?? data.Signal;
      this.callAnswered.set({ answererId, signal });
    });

    this.hubConnection.on('IceCandidateReceived', (data: any) => {
      const senderId: string = data.senderId ?? data.SenderId;
      const signal: string   = data.signal   ?? data.Signal;
      this.iceCandidateReceived.set({ senderId, signal });
    });

    this.hubConnection.on('CallEnded', (callerId: string) => {
      this.callEnded.set(callerId);
    });

    // ── Group Management ──────────────────────────────────────────────────
    this.hubConnection.on('GroupRenamed', (conversationId: string, newGroupName: string) => {
      this.inbox.update(inbox => {
        const conv = inbox.find(c => c.conversationId === conversationId);
        if (conv) conv.groupName = newGroupName;
        return [...inbox];
      });
      this.groupRenamed.set({ conversationId, newGroupName });
    });

    this.hubConnection.on('AddedToGroup', (conversationId: string) => {
      this.refreshInbox();
    });

    this.hubConnection.on('RemovedFromGroup', (conversationId: string) => {
      this.refreshInbox();
    });

    this.hubConnection.on('GroupParticipantsChanged', (conversationId: string) => {
      this.refreshInbox();
    });

    this.hubConnection.on('GroupDeleted', (conversationId: string) => {
      this.inbox.update(inbox => inbox.filter(c => c.conversationId !== conversationId));
      if (this._activeConversationMessages().length > 0 && this._activeConversationMessages()[0].conversationId === conversationId) {
        this._activeConversationMessages.set([]);
      }
    });

    // ── Message Updates ───────────────────────────────────────────────────
    this.hubConnection.on('MessageReactionChanged', (conversationId: string, messageId: string, userId: string, userName: string, emoji: string, reactions: MessageReactionDto[]) => {
      this._activeConversationMessages.update(msgs => {
        if (msgs.length > 0 && msgs[0].conversationId === conversationId) {
          return msgs.map(m => {
            if (m.id === messageId) {
              return { ...m, reactions: reactions };
            }
            return m;
          });
        }
        return msgs;
      });
    });

    this.hubConnection.on('MessageDeleted', (conversationId: string, messageId: string) => {
      this._activeConversationMessages.update(msgs => {
        if (msgs.length > 0 && msgs[0].conversationId === conversationId) {
          return msgs.map(m => {
            if (m.id === messageId) {
              return { ...m, isDeleted: true, content: '', attachmentUrl: undefined, attachmentType: AttachmentType.None, reactions: [] };
            }
            return m;
          });
        }
        return msgs;
      });
      // Also check inbox if this was the last message preview
      this.inbox.update(inbox => {
        return inbox.map(conv => {
          if (conv.conversationId === conversationId && conv.lastMessagePreview !== 'This message was deleted') {
             // In a perfect world, we'd fetch the previous message. For now, we refresh the inbox from the server.
             // We'll call refreshInbox() below, so this local update is just immediate visual feedback if it matched.
          }
          return conv;
        });
      });
      this.refreshInbox();
    });

  }

  startConnection(): void {
    if (this.hubConnection) return;

    const token = this.authService.getAccessToken();
    if (!token) return;

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/chat';

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.setupSignalREvents();

    this.hubConnection.onreconnecting(() => this.isConnected.set(false));
    this.hubConnection.onreconnected(() => {
      this.isConnected.set(true);
      this.refreshInbox();
    });
    this.hubConnection.onclose(() => this.isConnected.set(false));

    this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        this.refreshInbox();

        // Get initial online users list from presence tracker
        this.hubConnection?.invoke('GetOnlineUsers').then((users: string[]) => {
          this._onlineUsers.set(users);
        }).catch(err => console.error('Error getting online users', err));
      })
      .catch(err => console.error('Error while starting Chat SignalR connection: ' + err));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected.set(false);
      this.incomingCall.set(null);
      this.callAnswered.set(null);
      this.iceCandidateReceived.set(null);
      this.callEnded.set(null);
      this.outgoingCallTarget.set(null);
    }
  }

  async sendMessage(request: SendMessageRequest): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') {
      this.toastr.error(this.translate.instant('CHAT_SERVICE.ERR_NOT_CONNECTED'), this.translate.instant('CHAT_SERVICE.ERR_TITLE'));
      return;
    }

    try {
      await this.hubConnection.invoke('SendMessage', request);
    } catch (err: any) {
      console.error('Error sending message: ', err);
      console.error('Error details: ', JSON.stringify(err, Object.getOwnPropertyNames(err)));
      this.toastr.error(this.translate.instant('CHAT_SERVICE.ERR_SEND_MSG'), this.translate.instant('CHAT_SERVICE.ERR_TITLE'));
    }
  }

  async sendGroupMessage(request: SendGroupMessageRequest): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') {
      this.toastr.error(this.translate.instant('CHAT_SERVICE.ERR_NOT_CONNECTED'), this.translate.instant('CHAT_SERVICE.ERR_TITLE'));
      return;
    }

    try {
      await this.hubConnection.invoke('SendGroupMessage', request);
    } catch (err: any) {
      console.error('Error sending group message: ', err);
      console.error('Error details: ', JSON.stringify(err, Object.getOwnPropertyNames(err)));
      this.toastr.error(this.translate.instant('CHAT_SERVICE.ERR_SEND_MSG'), this.translate.instant('CHAT_SERVICE.ERR_TITLE'));
    }
  }

  async sendAttachmentMessage(recipientId: string, content: string, attachmentUrl: string, attachmentType: AttachmentType, repliedToMessageId?: string): Promise<void> {
    await this.sendMessage({
      recipientId,
      content,
      attachmentUrl,
      attachmentType,
      repliedToMessageId
    });
  }

  async markAsDelivered(senderId: string, conversationId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    try {
      await this.hubConnection.invoke('MarkAsDelivered', senderId, conversationId);
    } catch (err) {
      console.error('Error marking as delivered', err);
    }
  }

  async markAsRead(conversationId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    try {
      await this.hubConnection.invoke('MarkAsRead', conversationId);
    } catch (err) {
      console.error('Error marking as read', err);
    }
  }

  // ── WebRTC Actions ────────────────────────────────────────────────────────
  
  /**
   * Triggers the global overlay to initiate an outgoing call.
   */
  startOutgoingCall(targetUserId: string): void {
    this.outgoingCallTarget.set(targetUserId);
  }

  async sendCallOffer(dto: WebRtcSignalDto): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    await this.hubConnection.invoke('SendCallOffer', dto);
  }

  async sendCallAnswer(dto: WebRtcSignalDto): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    await this.hubConnection.invoke('SendCallAnswer', dto);
  }

  async sendIceCandidate(dto: WebRtcSignalDto): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    await this.hubConnection.invoke('SendIceCandidate', dto);
  }

  async endCall(targetUserId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    await this.hubConnection.invoke('EndCall', targetUserId);
  }

  async notifyTyping(conversationId: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') return;
    try {
      await this.hubConnection.invoke('NotifyTyping', conversationId);
    } catch (err) {
      // Ignore typing errors silently
    }
  }

  // ── State Management ──────────────────────────────────────────────────────

  refreshInbox(): void {
    this.loadInbox().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.inbox.set(res.data);
          this.updateTotalUnreadCount(res.data);
        }
      }
    });
  }

  /**
   * Sets the active conversation messages and triggers MarkAsRead so delivery
   * receipts update in real-time on the sender's side.
   */
  setActiveConversationMessages(messages: ChatMessageDto[], conversationId?: string): void {
    // REST API converts enums to strings due to JsonStringEnumConverter. 
    // Coerce them back to integers to align with the MessageStatus & AttachmentType enum values used in HTML bindings.
    messages.forEach(m => {
      if (typeof m.status === 'string') {
        m.status = MessageStatus[m.status as keyof typeof MessageStatus];
      }
      if (typeof m.attachmentType === 'string') {
        m.attachmentType = AttachmentType[m.attachmentType as keyof typeof AttachmentType];
      }
    });
    
    this._activeConversationMessages.set(messages);
    this.refreshInbox();
    // Notify the server that all messages in this conversation are now Read
    if (conversationId) {
      this.markAsRead(conversationId);
    }
  }

  private handleIncomingMessage(message: ChatMessageDto): void {
    const currentUserId = this.authService.currentUser()?.userId;
    
    // If we have an active conversation and the message belongs to it, append it
    const currentMsgs = this.activeConversationMessages();
    if (currentMsgs.length > 0 && currentMsgs[0].conversationId === message.conversationId) {
      this._activeConversationMessages.update(msgs => [...msgs, message]);
      
      // If the incoming message is from someone else, mark the whole conversation as Read immediately
      if (message.senderId !== currentUserId) {
        this.markAsRead(message.conversationId);
      }
    } else if (message.conversationId && currentMsgs.length === 0) {
      // First message in a new conversation that is currently open
      const activeMsgs = this.activeConversationMessages();
      if (activeMsgs.length === 0 && message.senderId !== currentUserId) {
        this.markAsDelivered(message.senderId, message.conversationId);
        this.toastr.info(
          `${message.senderName}: ${message.content || '📎 Attachment'}`,
          this.translate.instant('CHAT_SERVICE.NEW_MSG_TITLE')
        );
      }
    } else {
      // Message is for a background (non-active) conversation
      if (message.senderId !== currentUserId) {
        this.markAsDelivered(message.senderId, message.conversationId);
        this.toastr.info(
          `${message.senderName}: ${message.content || '📎 Attachment'}`,
          this.translate.instant('CHAT_SERVICE.NEW_MSG_TITLE')
        );
      }
    }

    // Refresh inbox to show latest message preview and update unread count
    this.refreshInbox();
  }

  private updateTotalUnreadCount(inbox: ConversationSummaryDto[]): void {
    const total = inbox.reduce((sum, conv) => sum + conv.unreadCount, 0);
    this._unreadTotalCount.set(total);
  }
}

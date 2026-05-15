import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { 
  ChatMessageDto, 
  ChatUserDto, 
  ConversationSummaryDto, 
  SendMessageRequest 
} from '../models/chat.model';
import { ToastrService } from 'ngx-toastr';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);

  private hubConnection: HubConnection | null = null;
  
  // ── Reactive State (Angular Signals) ──────────────────────────────────────
  readonly inbox = signal<ConversationSummaryDto[]>([]);
  readonly activeConversationMessages = signal<ChatMessageDto[]>([]);
  readonly unreadTotalCount = signal<number>(0);
  readonly isConnected = signal<boolean>(false);

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

  // ── SignalR Methods ──────────────────────────────────────────────────────

  startConnection(): void {
    if (this.hubConnection?.state === 'Connected') return;

    const token = this.authService.getAccessToken();
    if (!token) return;

    const hubUrl = environment.apiUrl.replace('/api', '') + '/hubs/chat';

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.hubConnection.on('ReceiveMessage', (message: ChatMessageDto) => {
      this.handleIncomingMessage(message);
    });

    this.hubConnection.onreconnecting(() => this.isConnected.set(false));
    this.hubConnection.onreconnected(() => this.isConnected.set(true));
    this.hubConnection.onclose(() => this.isConnected.set(false));

    this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        this.refreshInbox();
      })
      .catch(err => console.error('Error while starting Chat SignalR connection: ' + err));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop();
      this.hubConnection = null;
      this.isConnected.set(false);
    }
  }

  async sendMessage(request: SendMessageRequest): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== 'Connected') {
      this.toastr.error('Not connected to chat server.', 'Error');
      return;
    }

    try {
      await this.hubConnection.invoke('SendMessage', request);
    } catch (err) {
      console.error('Error sending message: ', err);
      this.toastr.error('Failed to send message.', 'Error');
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

  setActiveConversationMessages(messages: ChatMessageDto[]): void {
    this.activeConversationMessages.set(messages);
    this.refreshInbox(); // Refresh inbox to clear unread counts for this conversation
  }

  private handleIncomingMessage(message: ChatMessageDto): void {
    const currentUserId = this.authService.currentUser()?.userId;
    
    // If we have an active conversation and the message belongs to it, append it
    const currentMsgs = this.activeConversationMessages();
    if (currentMsgs.length > 0 && currentMsgs[0].conversationId === message.conversationId) {
      this.activeConversationMessages.update(msgs => [...msgs, message]);
      
      // If it's not my own message, we might want to mark it as read, but for now
      // standard behavior is they need to refresh to clear the server side unread, 
      // or we just call loadMessages again. To keep it simple, we just append it.
      if (message.senderId !== currentUserId) {
        // play a sound or just show notification if window is not focused
      }
    } else {
      // If the message is for another conversation and from someone else
      if (message.senderId !== currentUserId) {
        this.toastr.info(`${message.senderName}: ${message.content}`, 'New Message');
      }
    }

    // Refresh inbox to show latest message preview and update unread count
    this.refreshInbox();
  }

  private updateTotalUnreadCount(inbox: ConversationSummaryDto[]): void {
    const total = inbox.reduce((sum, conv) => sum + conv.unreadCount, 0);
    this.unreadTotalCount.set(total);
  }
}

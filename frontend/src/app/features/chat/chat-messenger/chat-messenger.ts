import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../../core/services/chat.service';
import { AuthService } from '../../../core/services/auth.service';
import { ChatMessageDto, ChatUserDto, ConversationSummaryDto, SendMessageRequest } from '../../../core/models/chat.model';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-chat-messenger',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-messenger.html',
  styleUrls: ['./chat-messenger.scss']
})
export class ChatMessengerComponent implements OnInit, OnDestroy {
  chatService = inject(ChatService);
  authService = inject(AuthService);

  // Component State
  activeConversation = signal<ConversationSummaryDto | null>(null);
  newMessageContent = signal<string>('');
  
  // New Chat Modal State
  isNewChatModalOpen = signal<boolean>(false);
  availableUsers = signal<ChatUserDto[]>([]);
  userSearchQuery = signal<string>('');
  selectedRoleFilter = signal<string>('All');

  ngOnInit(): void {
    this.chatService.startConnection();
  }

  ngOnDestroy(): void {
    // Only stop connection when destroying component
    // If it's a global widget, we wouldn't stop it here. But this is a dedicated page.
    // Actually, maybe we keep it connected across the app if we want real-time unread counts globally?
    // The instructions say "chat-messenger standalone component". 
    // If we want global unread counts, ChatService should start connection in AppComponent.
    // But let's follow standard component lifecycle for now.
    // Actually we will leave it running so they keep getting toasts while exploring the app?
    // Let's just keep the connection alive if we want.
    // We will stop it here for isolation.
    this.chatService.stopConnection();
  }

  openConversation(conv: ConversationSummaryDto): void {
    this.activeConversation.set(conv);
    this.chatService.loadMessages(conv.conversationId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.chatService.setActiveConversationMessages(res.data);
        }
      }
    });
  }

  async sendMessage(): Promise<void> {
    const content = this.newMessageContent().trim();
    if (!content) return;

    const conv = this.activeConversation();
    if (!conv) return;

    const request: SendMessageRequest = {
      recipientId: conv.otherUserId,
      content: content
    };

    await this.chatService.sendMessage(request);
    this.newMessageContent.set('');
  }

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

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
  }

  startChatWithUser(user: ChatUserDto): void {
    this.closeNewChatModal();
    // Check if conversation already exists
    this.chatService.getConversationId(user.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          // Exists
          const convId = res.data;
          const inbox = this.chatService.inbox();
          const existing = inbox.find(c => c.conversationId === convId);
          if (existing) {
            this.openConversation(existing);
          } else {
            // Need to reload inbox or simulate summary
            this.chatService.refreshInbox();
            // Create a fake summary until inbox refreshes
            this.openConversation({
              conversationId: convId,
              otherUserId: user.id,
              otherUserName: user.fullName,
              otherUserAvatarUrl: user.avatarUrl,
              otherUserRole: user.role,
              lastMessagePreview: '',
              lastMessageAt: new Date().toISOString(),
              unreadCount: 0
            });
          }
        } else {
          // Does not exist, create dummy active conversation object
          // It will be created on server when first message is sent.
          this.activeConversation.set({
            conversationId: 'NEW',
            otherUserId: user.id,
            otherUserName: user.fullName,
            otherUserAvatarUrl: user.avatarUrl,
            otherUserRole: user.role,
            lastMessagePreview: '',
            lastMessageAt: new Date().toISOString(),
            unreadCount: 0
          });
          this.chatService.setActiveConversationMessages([]);
        }
      }
    });
  }

  filteredUsers() {
    const q = this.userSearchQuery().toLowerCase();
    const role = this.selectedRoleFilter();
    return this.availableUsers().filter(u => {
      const matchesSearch = u.fullName.toLowerCase().includes(q);
      const matchesRole = role === 'All' || u.role === role;
      return matchesSearch && matchesRole;
    });
  }

  getAvatarUrl(path: string | undefined | null): string | null {
    if (!path) return null;
    // If it's already an absolute URL (e.g. from Google login), return as is
    if (path.startsWith('http')) return path;
    const apiUrl = environment.apiUrl.replace('/api', '');
    return `${apiUrl}/${path}`;
  }
}

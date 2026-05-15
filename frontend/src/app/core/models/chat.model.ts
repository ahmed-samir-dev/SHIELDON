export interface SendMessageRequest {
  recipientId: string;
  content: string;
}

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  senderAvatarUrl?: string;
  content: string;
  isRead: boolean;
  sentAt: string;
  isOwnMessage: boolean;
}

export interface ConversationSummaryDto {
  conversationId: string;
  otherUserId: string;
  otherUserName: string;
  otherUserAvatarUrl?: string;
  otherUserRole: string;
  lastMessagePreview: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface ChatUserDto {
  id: string;
  fullName: string;
  avatarUrl?: string;
  role: string;
}

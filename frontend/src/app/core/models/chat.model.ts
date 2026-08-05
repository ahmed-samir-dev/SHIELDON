export enum MessageStatus {
  Sent = 0,
  Delivered = 1,
  Read = 2
}

export interface LinkPreviewData {
  url: string;
  title?: string;
  description?: string;
  image?: string;
  siteName?: string;
}


export enum AttachmentType {
  None = 0,
  Audio = 1,
  Image = 2,
  Document = 3
}

export interface SendMessageRequest {
  recipientId: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: AttachmentType;
  repliedToMessageId?: string;
}

export interface SendGroupMessageRequest {
  conversationId: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: AttachmentType;
  repliedToMessageId?: string;
}

export interface ChatMessageDto {
  id: string;
  conversationId: string;
  senderId: string;
  senderName: string;
  senderAvatarUrl?: string;
  content: string;
  status: MessageStatus;
  attachmentType: AttachmentType;
  attachmentUrl?: string;
  sentAt: string;
  isOwnMessage: boolean;
  isDeleted?: boolean;
  isForwarded?: boolean;
  repliedToMessageId?: string;
  repliedToMessageContent?: string;
  repliedToMessageSenderName?: string;
  repliedToMessageAttachmentType?: AttachmentType;
  reactions?: MessageReactionDto[];
}

export interface MessageReactionDto {
  userId: string;
  userName: string;
  emoji: string;
}

export interface ReactToMessageRequest {
  emoji: string;
}

export interface ForwardMessageRequest {
  messageId: string;
  targetConversationIds: string[];
}

export interface ConversationSummaryDto {
  conversationId: string;
  otherUserId?: string;
  otherUserName?: string;
  otherUserAvatarUrl?: string;
  otherUserRole?: string;
  isGroup: boolean;
  groupName?: string;
  groupIconUrl?: string;
  lastMessagePreview: string;
  lastMessageAt: string;
  unreadCount: number;
  otherUserLastSeenAt?: string;
}

export interface ChatUserDto {
  id: string;
  fullName: string;
  avatarUrl?: string;
  role: string;
  isOnline: boolean;
  lastSeenAt?: string;
}

export interface CreateGroupRequest {
  groupName: string;
  memberIds: string[];
}

export interface RenameGroupRequest {
  newGroupName: string;
}

export interface AddGroupMembersRequest {
  memberIds: string[];
}

export interface AttachmentUploadResponse {
  url: string;
  attachmentType: AttachmentType;
}

export interface WebRtcSignalDto {
  targetUserId: string;
  signal: string;
}

export interface GroupParticipantDto {
  userId: string;
  fullName: string;
  avatarUrl?: string;
  isAdmin: boolean;
}


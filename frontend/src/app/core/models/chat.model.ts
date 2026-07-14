export enum MessageStatus {
  Sent = 0,
  Delivered = 1,
  Read = 2
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
}

export interface SendGroupMessageRequest {
  conversationId: string;
  content: string;
  attachmentUrl?: string;
  attachmentType?: AttachmentType;
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
}

export interface ChatUserDto {
  id: string;
  fullName: string;
  avatarUrl?: string;
  role: string;
  isOnline: boolean;
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

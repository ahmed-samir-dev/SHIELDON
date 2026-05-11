export interface MaterialResponse {
  id: string;
  courseId: string;
  title: string;
  description: string | null;
  materialType: 'File' | 'Link';
  originalFileName: string | null;
  contentType: string | null;
  fileSizeBytes: number | null;
  externalUrl: string | null;
  uploadedByUserId: string;
  uploadedByName: string;
  createdAt: string;
}

export interface AddMaterialRequest {
  title: string;
  description: string | null;
  materialType: 'File' | 'Link';
  externalUrl: string | null;
  // file property is not strongly typed here as it's sent via FormData
}

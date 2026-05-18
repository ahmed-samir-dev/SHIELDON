export interface PaymentRecordDto {
  id: string;
  courseId: string;
  courseName: string;
  amountUSD: number;
  status: 'Pending' | 'Processing' | 'Paid' | 'Failed';
  paidAt: string | null;
  createdAt: string;
  studentName?: string;
  studentDisplayId?: string;
}

export interface PaymentHistoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  status?: string;
}

export interface CheckoutSessionResponse {
  checkoutUrl: string;
  sessionId: string;
}

export interface CreateCheckoutSessionRequest {
  paymentRecordId: string;
}

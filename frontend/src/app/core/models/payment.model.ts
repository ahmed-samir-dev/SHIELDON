export interface PaymentRecordDto {
  id: string;
  courseId: string;
  courseName: string;
  amountUSD: number;
  status: 'Pending' | 'Processing' | 'Paid' | 'Failed';
  paidAt: string | null;
  createdAt: string;
}

export interface CheckoutSessionResponse {
  checkoutUrl: string;
  sessionId: string;
}

export interface CreateCheckoutSessionRequest {
  paymentRecordId: string;
}

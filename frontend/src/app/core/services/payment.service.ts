import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { PaymentRecordDto, CheckoutSessionResponse, CreateCheckoutSessionRequest } from '../models/payment.model';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/payment`;

  constructor(private http: HttpClient) {}

  getPendingPayments(): Observable<PaymentRecordDto[]> {
    return this.http.get<ApiResponse<PaymentRecordDto[]>>(`${this.apiUrl}/pending`)
      .pipe(map(response => response.data));
  }

  createCheckoutSession(paymentRecordId: string): Observable<CheckoutSessionResponse> {
    const request: CreateCheckoutSessionRequest = { paymentRecordId };
    return this.http.post<ApiResponse<CheckoutSessionResponse>>(`${this.apiUrl}/checkout`, request)
      .pipe(map(response => response.data));
  }
}

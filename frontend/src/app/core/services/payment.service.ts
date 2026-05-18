import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { PaymentRecordDto, CheckoutSessionResponse, CreateCheckoutSessionRequest, PaymentHistoryQueryParams } from '../models/payment.model';
import { HttpParams } from '@angular/common/http';

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

  getPaymentHistory(params: PaymentHistoryQueryParams): Observable<PagedResponse<PaymentRecordDto>> {
    let httpParams = new HttpParams();
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params.search) httpParams = httpParams.set('search', params.search);
    if (params.status) httpParams = httpParams.set('status', params.status);

    return this.http.get<ApiResponse<PagedResponse<PaymentRecordDto>>>(`${this.apiUrl}/history`, { params: httpParams })
      .pipe(map(response => response.data));
  }
}

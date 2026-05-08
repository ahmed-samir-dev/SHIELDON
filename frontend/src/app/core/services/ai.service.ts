import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { ApiResponse } from '../../core/models/api-response.model';

export interface ChatMessage {
  role: 'user' | 'model';
  content: string;
  timestamp: Date;
}

@Injectable({
  providedIn: 'root'
})
export class AiService {
  private readonly apiUrl = `${environment.apiUrl}/ai/chat`;

  private chatHistorySignal = signal<ChatMessage[]>([]);
  public chatHistory = this.chatHistorySignal.asReadonly();
  
  public isTyping = signal<boolean>(false);

  constructor(private http: HttpClient) {}

  sendMessage(message: string): Observable<ApiResponse<{ reply: string }>> {
    this.chatHistorySignal.update(history => [
      ...history,
      { role: 'user', content: message, timestamp: new Date() }
    ]);
    
    this.isTyping.set(true);

    const payload = {
      message,
      history: this.chatHistorySignal().slice(0, -1).map(h => ({ role: h.role, content: h.content }))
    };

    return this.http.post<ApiResponse<{ reply: string }>>(this.apiUrl, payload).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.chatHistorySignal.update(history => [
            ...history,
            { role: 'model', content: response.data!.reply, timestamp: new Date() }
          ]);
        }
        this.isTyping.set(false);
      }),
      catchError(err => {
        this.isTyping.set(false);
        return throwError(() => err);
      })
    );
  }

  clearHistory() {
    this.chatHistorySignal.set([]);
  }
}

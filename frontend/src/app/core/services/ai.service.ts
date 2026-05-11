import { Injectable, signal, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { ApiResponse } from '../../core/models/api-response.model';

export interface ChatMessage {
  role: 'user' | 'model';
  content: string;
  timestamp: Date;
}

@Injectable()
export class AiService {
  private readonly apiUrl = `${environment.apiUrl}/ai/chat`;

  private chatHistorySignal = signal<ChatMessage[]>([]);
  public chatHistory = this.chatHistorySignal.asReadonly();
  
  public isTyping = signal<boolean>(false);

  private authService = inject(AuthService);
  
  private get storageKey() {
    const user = this.authService.currentUser();
    return user ? `shieldon_ai_history_${user.userId}` : null;
  }

  constructor(private http: HttpClient) {
    this.loadHistory();
  }

  private loadHistory() {
    const key = this.storageKey;
    if (key) {
      const stored = localStorage.getItem(key);
      if (stored) {
        try {
          const parsed = JSON.parse(stored);
          parsed.forEach((m: any) => m.timestamp = new Date(m.timestamp));
          this.chatHistorySignal.set(parsed);
        } catch (e) {
          console.error('Failed to parse AI history', e);
        }
      }
    }
  }

  private saveHistory() {
    const key = this.storageKey;
    if (key) {
      localStorage.setItem(key, JSON.stringify(this.chatHistorySignal()));
    }
  }

  sendMessage(message: string): Observable<ApiResponse<{ reply: string }>> {
    this.chatHistorySignal.update(history => [
      ...history,
      { role: 'user', content: message, timestamp: new Date() }
    ]);
    this.saveHistory();
    
    this.isTyping.set(true);

    const payload = {
      message,
      history: this.chatHistorySignal().slice(0, -1).map(h => ({ role: h.role, content: h.content }))
    };

    return this.http.post<ApiResponse<{ reply: string }>>(this.apiUrl, payload, {
      headers: new HttpHeaders({ 'X-Silent': 'true' })
    }).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.chatHistorySignal.update(history => [
            ...history,
            { role: 'model', content: response.data!.reply, timestamp: new Date() }
          ]);
          this.saveHistory();
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
    const key = this.storageKey;
    if (key) {
      localStorage.removeItem(key);
    }
  }
}

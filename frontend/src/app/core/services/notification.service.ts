import { Injectable, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResponse } from '../models/api-response.model';
import { NotificationResponse } from '../models/notification.model';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly apiUrl = `${environment.apiUrl}/notifications`;

  // Signals for global state
  public unreadCount = signal<number>(0);
  public notifications = signal<NotificationResponse[]>([]);
  public isLoading = signal<boolean>(false);
  public totalItems = signal<number>(0);

  constructor(private http: HttpClient) {}

  // Fetch unread count to show on the bell badge
  fetchUnreadCount(): void {
    this.http.get<ApiResponse<number>>(`${this.apiUrl}/unread-count`)
      .subscribe({
        next: (res) => {
          if (res.success && res.data !== undefined) {
            this.unreadCount.set(res.data);
          }
        },
        error: (err) => console.error('Failed to load unread count', err)
      });
  }

  // Fetch paginated notifications
  fetchNotifications(page: number = 1, pageSize: number = 10, append: boolean = false): void {
    if (!append) this.isLoading.set(true);

    let params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', pageSize.toString());

    this.http.get<ApiResponse<PagedResponse<NotificationResponse>>>(this.apiUrl, { params })
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.totalItems.set(res.data.totalCount);
            if (append) {
              this.notifications.update(n => [...n, ...res.data!.items]);
            } else {
              this.notifications.set(res.data.items);
            }
          }
        },
        error: (err) => console.error('Failed to load notifications', err),
        complete: () => this.isLoading.set(false)
      });
  }

  // Mark a single notification as read
  markAsRead(id: string): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/read`, {}).pipe(
      tap(() => {
        // Optimistically update signals
        this.notifications.update(list => 
          list.map(n => n.id === id ? { ...n, isRead: true } : n)
        );
        this.unreadCount.update(c => Math.max(0, c - 1));
      })
    );
  }

  // Mark all notifications as read
  markAllAsRead(): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/mark-all-read`, {}).pipe(
      tap(() => {
        // Optimistically update signals
        this.notifications.update(list => 
          list.map(n => ({ ...n, isRead: true }))
        );
        this.unreadCount.set(0);
      })
    );
  }

  // Delete all notifications
  deleteAll(): Observable<void> {
    return this.http.delete<void>(this.apiUrl).pipe(
      tap(() => {
        // Optimistically update signals
        this.notifications.set([]);
        this.unreadCount.set(0);
        this.totalItems.set(0);
      })
    );
  }
}

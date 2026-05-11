import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import { MaterialResponse } from '../../../core/models/material.model';

@Injectable({
  providedIn: 'root'
})
export class MaterialService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  getMaterials(courseId: string): Observable<ApiResponse<MaterialResponse[]>> {
    return this.http.get<ApiResponse<MaterialResponse[]>>(`${this.apiUrl}/courses/${courseId}/materials`);
  }

  addMaterial(courseId: string, formData: FormData): Observable<ApiResponse<MaterialResponse>> {
    return this.http.post<ApiResponse<MaterialResponse>>(`${this.apiUrl}/courses/${courseId}/materials`, formData);
  }

  deleteMaterial(courseId: string, materialId: string): Observable<ApiResponse<void>> {
    return this.http.delete<ApiResponse<void>>(`${this.apiUrl}/courses/${courseId}/materials/${materialId}`);
  }

  downloadMaterial(courseId: string, materialId: string): void {
    // We open the download link in a new tab.
    // However, since it's an authenticated endpoint, browser navigation might fail without the token.
    // Instead, we will fetch it as a blob and then trigger download.
    this.http.get(`${this.apiUrl}/courses/${courseId}/materials/${materialId}/download`, {
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const contentDisposition = response.headers.get('Content-Disposition');
        let filename = 'download';
        if (contentDisposition) {
          const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
          const matches = filenameRegex.exec(contentDisposition);
          if (matches != null && matches[1]) {
            filename = matches[1].replace(/['"]/g, '');
          }
        }
        
        const blob = response.body;
        if (blob) {
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = filename;
          document.body.appendChild(a);
          a.click();
          window.URL.revokeObjectURL(url);
          document.body.removeChild(a);
        }
      },
      error: (err) => {
        console.error('Download failed', err);
        // Let the caller handle the error display if needed, or handle it via interceptor
      }
    });
  }
}

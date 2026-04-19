import { Component, Input, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MaterialService } from '../services/material.service';
import { AuthService } from '../../../core/services/auth.service';
import { MaterialResponse } from '../../../core/models/material.model';
import { ToastrService } from 'ngx-toastr';
import { CourseDetailResponse } from '../../../core/models/courses.model';
import Swal from 'sweetalert2';

@Component({
  selector: 'app-course-materials',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './course-materials.html',
  styleUrls: ['./course-materials.scss']
})
export class CourseMaterialsComponent implements OnInit {
  @Input() course!: CourseDetailResponse;

  private readonly materialService = inject(MaterialService);
  private readonly authService = inject(AuthService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly toastr = inject(ToastrService);

  materials = signal<MaterialResponse[]>([]);
  isLoading = signal<boolean>(true);
  isSubmitting = signal<boolean>(false);
  showUploadForm = signal<boolean>(false);
  
  uploadForm!: FormGroup;
  selectedFile: File | null = null;
  fileError = signal<string | null>(null);

  // Computed properties for access control
  canManageMaterials = computed(() => {
    if (this.authService.isAdmin()) return true;
    if (this.authService.isTutor() && this.course.assignedTutorId === this.authService.currentUser()?.userId) return true;
    return false;
  });

  // Calculate bytes to MB
  formatBytes(bytes: number, decimals = 2) {
    if (!+bytes) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(dm))} ${sizes[i]}`;
  }

  ngOnInit(): void {
    this.loadMaterials();
    this.initForm();
  }

  private initForm(): void {
    this.uploadForm = this.formBuilder.group({
      title: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
      materialType: ['File', Validators.required],
      externalUrl: ['']
    });

    // Update validators based on materialType
    this.uploadForm.get('materialType')?.valueChanges.subscribe(type => {
      const urlControl = this.uploadForm.get('externalUrl');
      if (type === 'Link') {
        urlControl?.setValidators([Validators.required, Validators.pattern(/^(http|https):\/\/[^ "]+$/)]);
        this.selectedFile = null;
        this.fileError.set(null);
      } else {
        urlControl?.clearValidators();
      }
      urlControl?.updateValueAndValidity();
    });
  }

  loadMaterials(): void {
    this.isLoading.set(true);
    this.materialService.getMaterials(this.course.id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.materials.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to load materials');
        this.isLoading.set(false);
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      
      // Basic validation
      if (file.size > 100 * 1024 * 1024) { // 100 MB
        this.fileError.set('File size exceeds 100 MB limit.');
        this.selectedFile = null;
        return;
      }
      
      this.fileError.set(null);
      this.selectedFile = file;
    }
  }

  toggleUploadForm(): void {
    this.showUploadForm.update((val) => !val);
    if (!this.showUploadForm()) {
      this.uploadForm.reset({ materialType: 'File' });
      this.selectedFile = null;
      this.fileError.set(null);
    }
  }

  onSubmit(): void {
    if (this.uploadForm.invalid) {
      this.uploadForm.markAllAsTouched();
      return;
    }

    const type = this.uploadForm.value.materialType;
    if (type === 'File' && !this.selectedFile) {
      this.fileError.set('Please select a file to upload.');
      return;
    }

    this.isSubmitting.set(true);
    const formData = new FormData();
    formData.append('title', this.uploadForm.value.title);
    formData.append('materialType', type);
    
    if (this.uploadForm.value.description) {
      formData.append('description', this.uploadForm.value.description);
    }

    if (type === 'Link') {
      formData.append('externalUrl', this.uploadForm.value.externalUrl);
    } else if (this.selectedFile) {
      formData.append('file', this.selectedFile);
    }

    this.materialService.addMaterial(this.course.id, formData).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.toastr.success(res.message || 'Material added successfully');
          this.materials.update(docs => [res.data!, ...docs]);
          this.toggleUploadForm();
        }
        this.isSubmitting.set(false);
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Failed to add material');
        this.isSubmitting.set(false);
      }
    });
  }

  downloadMaterial(material: MaterialResponse): void {
    if (material.materialType === 'Link' && material.externalUrl) {
      window.open(material.externalUrl, '_blank');
      return;
    }
    
    // It's a file, fetch via service
    this.toastr.info('Starting download...', 'Please wait', { timeOut: 2000 });
    this.materialService.downloadMaterial(this.course.id, material.id);
  }

  deleteMaterial(materialId: string): void {
    Swal.fire({
      title: 'Delete Material?',
      text: "This action cannot be undone.",
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#EF4444',
      cancelButtonColor: '#87949C',
      confirmButtonText: 'Yes, delete it'
    }).then((result) => {
      if (result.isConfirmed) {
        this.materialService.deleteMaterial(this.course.id, materialId).subscribe({
          next: () => {
            this.toastr.success('Material deleted successfully');
            this.materials.update(docs => docs.filter(m => m.id !== materialId));
          },
          error: (err) => {
            this.toastr.error(err.error?.message || 'Failed to delete material');
          }
        });
      }
    });
  }

  getFileIcon(contentType: string | null): string {
    if (!contentType) return 'file';
    if (contentType.includes('pdf')) return 'file-text';
    if (contentType.includes('image')) return 'image';
    if (contentType.includes('video')) return 'video';
    if (contentType.includes('word') || contentType.includes('document')) return 'file-text';
    if (contentType.includes('presentation') || contentType.includes('powerpoint')) return 'monitor-play';
    if (contentType.includes('spreadsheet') || contentType.includes('excel')) return 'table';
    if (contentType.includes('zip') || contentType.includes('compressed')) return 'archive';
    return 'file';
  }
}

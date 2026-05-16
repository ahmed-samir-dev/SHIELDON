import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { trigger, transition, style, animate } from '@angular/animations';
import { LucideAngularModule, X, Calendar, Type, AlignLeft, BookOpen, Clock } from 'lucide-angular';
import { CalendarEventDto, CreateCustomEventRequest, UpdateCustomEventRequest, EventType } from '../../../../core/models/calendar.model';
import { CourseResponse } from '../../../../core/models/courses.model';

@Component({
  selector: 'app-event-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LucideAngularModule],
  templateUrl: './event-modal.component.html',
  styleUrl: './event-modal.component.scss',
  animations: [
    trigger('modalAnimation', [
      transition(':enter', [
        style({ opacity: 0, transform: 'scale(0.95)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'scale(1)' }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'scale(0.95)' }))
      ])
    ]),
    trigger('backdropAnimation', [
      transition(':enter', [
        style({ opacity: 0 }),
        animate('200ms ease-out', style({ opacity: 1 }))
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0 }))
      ])
    ])
  ]
})
export class EventModalComponent implements OnInit {
  @Input() event: CalendarEventDto | null = null;
  @Input() courses: CourseResponse[] = [];
  @Input() initialDate: Date | null = null;
  @Input() isAdmin: boolean = false;
  
  @Output() save = new EventEmitter<CreateCustomEventRequest | UpdateCustomEventRequest>();
  @Output() delete = new EventEmitter<string>();
  @Output() close = new EventEmitter<void>();

  private fb = inject(FormBuilder);
  
  eventForm!: FormGroup;
  isEditMode = false;
  readonly icons = { X, Calendar, Type, AlignLeft, BookOpen, Clock };

  ngOnInit(): void {
    this.isEditMode = !!this.event;
    
    // Format dates for input[type="datetime-local"]
    let startDateStr = '';
    let endDateStr = '';
    
    if (this.event) {
      startDateStr = this.formatDateForInput(new Date(this.event.startDate));
      endDateStr = this.event.endDate ? this.formatDateForInput(new Date(this.event.endDate)) : '';
    } else if (this.initialDate) {
      // Set default times: current selected day, 09:00 to 10:00
      const start = new Date(this.initialDate);
      start.setHours(9, 0, 0, 0);
      const end = new Date(this.initialDate);
      end.setHours(10, 0, 0, 0);
      
      startDateStr = this.formatDateForInput(start);
      endDateStr = this.formatDateForInput(end);
    }

    this.eventForm = this.fb.group({
      title: [this.event?.title || '', [Validators.required, Validators.maxLength(200)]],
      description: [this.event?.description || '', [Validators.maxLength(1000)]],
      eventDate: [startDateStr, [Validators.required]],
      eventEndDate: [endDateStr],
      courseId: [this.event?.courseId || '', this.isAdmin ? [] : [Validators.required]]
    });
  }

  private formatDateForInput(date: Date): string {
    // Format to YYYY-MM-DDTHH:mm
    const pad = (num: number) => num.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  onSubmit(): void {
    if (this.eventForm.valid) {
      const formValue = this.eventForm.value;
      
      const payload: CreateCustomEventRequest | UpdateCustomEventRequest = {
        title: formValue.title,
        description: formValue.description || undefined,
        eventDate: new Date(formValue.eventDate).toISOString(),
        eventEndDate: formValue.eventEndDate ? new Date(formValue.eventEndDate).toISOString() : undefined,
        courseId: formValue.courseId || undefined
      };

      this.save.emit(payload);
    } else {
      this.eventForm.markAllAsTouched();
    }
  }

  onDelete(): void {
    if (this.event && this.event.id) {
      this.delete.emit(this.event.id);
    }
  }
}

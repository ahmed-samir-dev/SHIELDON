import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, EventApi, EventInput, DateSelectArg, EventClickArg } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import { ToastrService } from 'ngx-toastr';
import { LucideAngularModule, Plus, Calendar as CalendarIcon, RefreshCw } from 'lucide-angular';

import { AuthService } from '../../../core/services/auth.service';
import { CalendarService } from '../../../core/services/calendar.service';
import { CourseService } from '../../courses/services/course.service';
import { CalendarEventDto, EventType, CreateCustomEventRequest, UpdateCustomEventRequest } from '../../../core/models/calendar.model';
import { CourseResponse } from '../../../core/models/courses.model';
import { EventModalComponent } from './event-modal/event-modal.component';
import Swal from 'sweetalert2';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-calendar-view',
  standalone: true,
  imports: [CommonModule, FormsModule, FullCalendarModule, LucideAngularModule, EventModalComponent, TranslateModule],
  templateUrl: './calendar-view.component.html',
  styleUrls: ['./calendar-view.component.scss']
})
export class CalendarViewComponent implements OnInit {
  private calendarService = inject(CalendarService);
  private courseService = inject(CourseService);
  private authService = inject(AuthService);
  private toastr = inject(ToastrService);
  public translate = inject(TranslateService);

  @ViewChild('calendar') calendarComponent: any;

  readonly icons = { Plus, CalendarIcon, RefreshCw };
  
  calendarVisible = true;
  calendarOptions: CalendarOptions = {
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    initialView: 'dayGridMonth',
    events: [], // Set dynamically
    weekends: true,
    editable: false, // We will handle custom edit via modal, exams/assignments are read-only
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    select: this.handleDateSelect.bind(this),
    eventClick: this.handleEventClick.bind(this),
    datesSet: this.handleDatesSet.bind(this),
    height: 'auto',
    eventTimeFormat: {
      hour: 'numeric',
      minute: '2-digit',
      meridiem: 'short'
    },
    eventContent: this.renderEventContent.bind(this)
  };

  // State
  isLoading = false;
  events: CalendarEventDto[] = [];
  courses: CourseResponse[] = []; // For the modal dropdown
  isAdmin = false;
  isTutor = false;
  
  legendFilters = {
    exam: true,
    assignment: true,
    courseEvent: true,
    globalEvent: true
  };
  
  // Modal State
  showModal = false;
  selectedEvent: CalendarEventDto | null = null;
  selectedDate: Date | null = null;
  
  // Keep track of current view range to re-fetch on save
  currentStartStr = '';
  currentEndStr = '';

  ngOnInit() {
    const user = this.authService.currentUser();
    this.isAdmin = user?.role === 'Admin';
    this.isTutor = user?.role === 'Tutor';
    
    // Load courses for all users so course name can show in the event details popup
    this.loadCourses();
  }

  loadCourses() {
    this.courseService.getCourses({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.courses = res.data.items;
        }
      }
    });
  }

  handleDatesSet(arg: any) {
    this.currentStartStr = arg.startStr;
    this.currentEndStr = arg.endStr;
    this.fetchEvents();
  }

  fetchEvents() {
    if (!this.currentStartStr || !this.currentEndStr) return;
    
    this.isLoading = true;
    this.calendarService.getEvents(this.currentStartStr, this.currentEndStr).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.events = res.data;
          this.mapEventsToCalendar();
        }
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error(this.translate.instant('CALENDAR_VIEW.ERR_LOAD_EVENTS'));
      }
    });
  }

  toggleFilter(type: 'exam' | 'assignment' | 'courseEvent' | 'globalEvent') {
    this.legendFilters[type] = !this.legendFilters[type];
    this.mapEventsToCalendar();
  }

  mapEventsToCalendar() {
    const filtered = this.events.filter(event => {
      if (event.type === EventType.Exam) return this.legendFilters.exam;
      if (event.type === EventType.Assignment) return this.legendFilters.assignment;
      if (event.type === EventType.Custom && event.courseId) return this.legendFilters.courseEvent;
      if (event.type === EventType.Custom && !event.courseId) return this.legendFilters.globalEvent;
      return true;
    });

    const calendarEvents: EventInput[] = filtered.map(event => {
      let color = '';
      let className = '';
      
      switch (event.type) {
        case EventType.Exam:
          color = '#ef4444'; // Red
          className = 'event-exam';
          break;
        case EventType.Assignment:
          color = '#f97316'; // Orange
          className = 'event-assignment';
          break;
        case EventType.Custom:
          if (event.courseId) {
            color = '#3b82f6'; // Blue for Course Specific
            className = 'event-custom';
          } else {
            color = '#10b981'; // Emerald for Global
            className = 'event-global';
          }
          break;
      }
      
      return {
        id: event.id,
        title: event.title,
        start: event.startDate,
        end: event.endDate || undefined,
        allDay: !event.startDate.includes('T'), // basic check
        display: 'block', // Force rectangle instead of dot
        backgroundColor: color,     // Solid specific color
        borderColor: color,         // Colored border
        textColor: '#ffffff',       // White text
        classNames: [className],
        extendedProps: {
          originalEvent: event
        }
      };
    });
    
    this.calendarOptions.events = calendarEvents;
  }

  renderEventContent(arg: any) {
    const originalEvent = arg.event.extendedProps.originalEvent;
    const eventType = originalEvent.type;
    let iconHtml = '';
    let typeLabel = '';
    
    if (eventType === EventType.Exam) {
      iconHtml = `<svg class="event-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>`;
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_EXAM');
    } else if (eventType === EventType.Assignment) {
      iconHtml = `<svg class="event-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path></svg>`;
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_ASSIGNMENT');
    } else {
      iconHtml = `<svg class="event-icon" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"></path></svg>`;
      typeLabel = originalEvent.courseId ? this.translate.instant('CALENDAR_VIEW.EVENT_COURSE') : this.translate.instant('CALENDAR_VIEW.EVENT_GLOBAL');
    }
    
    // Show only time (HH:MM AM/PM) - the day/month is visible in the calendar grid already
    const dateObj = new Date(originalEvent.startDate);
    let hours = dateObj.getHours();
    const minutes = dateObj.getMinutes().toString().padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12;
    hours = hours ? hours : 12; // midnight → 12
    
    const timeText = `<span class="event-time">${hours}:${minutes} ${ampm}</span>`;
    
    return {
      html: `<div class="event-content-wrapper">
              ${iconHtml} ${timeText} <span class="event-title-text">${typeLabel}</span>
             </div>`
    };
  }

  handleDateSelect(selectInfo: DateSelectArg) {
    if (!this.isAdmin && !this.isTutor) return;
    
    const calendarApi = selectInfo.view.calendar;
    calendarApi.unselect(); // clear date selection
    
    this.openModal(null, selectInfo.start);
  }

  handleEventClick(clickInfo: EventClickArg) {
    const originalEvent: CalendarEventDto = clickInfo.event.extendedProps['originalEvent'];
    // Re-read role from auth service every click to avoid stale closure values
    const user = this.authService.currentUser();
    const isAdmin = user?.role === 'Admin';
    const isTutor = user?.role === 'Tutor';
    
    // Only Admin can edit Global Events. Tutors can edit Course Events.
    const isGlobalEvent = !originalEvent.courseId;
    const canEdit = isAdmin || (isTutor && !isGlobalEvent);
    
    if (originalEvent.type === EventType.Custom && canEdit) {
      this.showEditOrViewPopup(originalEvent);
    } else {
      this.showEventDetailsPopup(originalEvent);
    }
  }

  showEditOrViewPopup(event: CalendarEventDto) {
    const isGlobal = !event.courseId;
    const typeLabel = isGlobal ? this.translate.instant('CALENDAR_VIEW.EVENT_GLOBAL') : this.translate.instant('CALENDAR_VIEW.EVENT_COURSE');
    const scopeLabel = isGlobal
      ? this.translate.instant('CALENDAR_VIEW.SCOPE_GLOBAL_DESC')
      : (event.courseName || this.translate.instant('CALENDAR_VIEW.EVENT_COURSE'));

    Swal.fire({
      title: `<span style="font-size:17px;font-weight:700;color:#111827">${event.title}</span>`,
      html: `
        <div style="text-align:left; font-size:14px; color:#374151">
          <span style="display:inline-block;padding:3px 12px;border-radius:999px;font-size:12px;font-weight:700;
            background:${isGlobal ? '#ecfdf5' : '#eff6ff'};color:${isGlobal ? '#059669' : '#1d4ed8'};
            border:1px solid ${isGlobal ? '#a7f3d0' : '#bfdbfe'};margin-bottom:14px">${typeLabel}</span>
          <div style="border:1px solid #e5e7eb;border-radius:10px;overflow:hidden">
            <div style="display:flex;gap:12px;padding:8px 12px;border-bottom:1px solid #f3f4f6">
              <span style="font-weight:600;min-width:80px;color:#374151;font-size:13px">${isGlobal ? this.translate.instant('CALENDAR_VIEW.SCOPE') : this.translate.instant('CALENDAR_VIEW.COURSE')}</span>
              <span style="color:#6b7280;font-size:13px">${scopeLabel}</span>
            </div>
            <div style="display:flex;gap:12px;padding:8px 12px;border-bottom:1px solid #f3f4f6">
              <span style="font-weight:600;min-width:80px;color:#374151;font-size:13px">${this.translate.instant('CALENDAR_VIEW.STARTS')}</span>
              <span style="color:#6b7280;font-size:13px">${new Date(event.startDate).toLocaleString('en-US', {dateStyle:'medium', timeStyle:'short'})}</span>
            </div>
            ${event.endDate ? `<div style="display:flex;gap:12px;padding:8px 12px">
              <span style="font-weight:600;min-width:80px;color:#374151;font-size:13px">${this.translate.instant('CALENDAR_VIEW.ENDS')}</span>
              <span style="color:#6b7280;font-size:13px">${new Date(event.endDate).toLocaleString('en-US', {dateStyle:'medium', timeStyle:'short'})}</span>
            </div>` : ''}
          </div>
          ${event.description ? `<div style="margin-top:12px;padding:10px 14px;background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb">
            <p style="font-weight:600;font-size:11px;color:#9ca3af;text-transform:uppercase;letter-spacing:.06em;margin-bottom:4px">${this.translate.instant('CALENDAR_VIEW.DESCRIPTION')}</p>
            <p style="font-size:13px;color:#6b7280;white-space:pre-wrap;margin:0">${event.description}</p>
          </div>` : ''}
        </div>`,
      showCancelButton: false,
      showDenyButton: true,
      showConfirmButton: true,
      confirmButtonText: this.translate.instant('CALENDAR_VIEW.BTN_EDIT'),
      denyButtonText: this.translate.instant('CALENDAR_VIEW.BTN_DELETE'),
      confirmButtonColor: '#3b82f6',
      denyButtonColor: '#ef4444',
      width: 460
    }).then((result) => {
      if (result.isConfirmed) {
        this.openModal(event, null);
      } else if (result.isDenied) {
        this.deleteEvent(event.id);
      }
    });
  }

  showEventDetailsPopup(event: CalendarEventDto) {
    const isExam = event.type === EventType.Exam;
    const isAssignment = event.type === EventType.Assignment;
    const isCustom = event.type === EventType.Custom;
    const isGlobal = isCustom && !event.courseId;

    let typeLabel: string;
    let badgeBg: string;
    let badgeColor: string;
    let badgeBorder: string;

    if (isExam) {
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_EXAM'); badgeBg = '#fef2f2'; badgeColor = '#dc2626'; badgeBorder = '#fecaca';
    } else if (isAssignment) {
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_ASSIGNMENT'); badgeBg = '#fff7ed'; badgeColor = '#ea580c'; badgeBorder = '#fed7aa';
    } else if (isGlobal) {
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_GLOBAL'); badgeBg = '#ecfdf5'; badgeColor = '#059669'; badgeBorder = '#a7f3d0';
    } else {
      typeLabel = this.translate.instant('CALENDAR_VIEW.EVENT_COURSE'); badgeBg = '#eff6ff'; badgeColor = '#1d4ed8'; badgeBorder = '#bfdbfe';
    }

    // Use courseName directly from the DTO (populated by the API)
    const courseName = event.courseName || null;

    const fmtDate = (d: string) => new Date(d).toLocaleString('en-US', { dateStyle: 'long', timeStyle: 'short' });
    const startStr = fmtDate(event.startDate);
    const endStr = event.endDate ? fmtDate(event.endDate) : null;

    const row = (label: string, value: string) =>
      `<div style="display:flex;gap:12px;padding:8px 12px;border-bottom:1px solid #f3f4f6">
        <span style="font-weight:600;min-width:90px;color:#374151;font-size:13px">${label}</span>
        <span style="color:#6b7280;font-size:13px;flex:1;text-align:left">${value}</span>
      </div>`;

    // Course row: skip entirely for Global Events; show name for others
    const courseRow = isGlobal
      ? row(this.translate.instant('CALENDAR_VIEW.SCOPE'), this.translate.instant('CALENDAR_VIEW.SCOPE_GLOBAL_DESC_FULL'))
      : (courseName ? row(this.translate.instant('CALENDAR_VIEW.COURSE'), courseName) : '');

    const htmlContent = `
      <div style="text-align:left">
        <span style="display:inline-block;padding:3px 12px;border-radius:999px;font-size:12px;font-weight:700;
               background:${badgeBg};color:${badgeColor};border:1px solid ${badgeBorder};margin-bottom:16px">${typeLabel}</span>
        <div style="border:1px solid #e5e7eb;border-radius:10px;overflow:hidden">
          ${courseRow}
          ${row(this.translate.instant('CALENDAR_VIEW.STARTS'), startStr)}
          ${endStr ? row(this.translate.instant('CALENDAR_VIEW.ENDS'), endStr) : ''}
        </div>
        ${event.description ? `
          <div style="margin-top:12px;padding:10px 14px;background:#f9fafb;border-radius:8px;border:1px solid #e5e7eb">
            <p style="font-weight:600;font-size:11px;color:#9ca3af;text-transform:uppercase;letter-spacing:.06em;margin-bottom:4px">${this.translate.instant('CALENDAR_VIEW.DESCRIPTION')}</p>
            <p style="font-size:13px;color:#6b7280;white-space:pre-wrap;margin:0">${event.description}</p>
          </div>` : ''}
      </div>`;

    Swal.fire({
      title: `<span style="font-size:18px;font-weight:700;color:#111827">${event.title}</span>`,
      html: htmlContent,
      confirmButtonText: this.translate.instant('CALENDAR_VIEW.BTN_CLOSE'),
      confirmButtonColor: '#3b82f6',
      width: 480
    });
  }

  openModal(event: CalendarEventDto | null = null, date: Date | null = null) {
    this.selectedEvent = event;
    this.selectedDate = date;
    this.showModal = true;
  }

  closeModal() {
    this.showModal = false;
    this.selectedEvent = null;
    this.selectedDate = null;
  }

  saveEvent(payload: CreateCustomEventRequest | UpdateCustomEventRequest) {
    if (this.selectedEvent) {
      // Update
      this.calendarService.updateCustomEvent(this.selectedEvent.id, payload as UpdateCustomEventRequest).subscribe({
        next: (res) => {
          if (res.success) {
            this.toastr.success(this.translate.instant('CALENDAR_VIEW.MSG_EVENT_UPDATED'));
            this.closeModal();
            this.fetchEvents(); // Refresh
          }
        },
        error: (err: any) => this.toastr.error(err.error?.message || this.translate.instant('CALENDAR_VIEW.ERR_UPDATE'))
      });
    } else {
      // Create
      this.calendarService.createCustomEvent(payload as CreateCustomEventRequest).subscribe({
        next: (res) => {
          if (res.success) {
            this.toastr.success(this.translate.instant('CALENDAR_VIEW.MSG_EVENT_CREATED'));
            this.closeModal();
            this.fetchEvents(); // Refresh
          }
        },
        error: (err: any) => this.toastr.error(err.error?.message || this.translate.instant('CALENDAR_VIEW.ERR_CREATE'))
      });
    }
  }

  deleteEvent(eventId: string) {
    Swal.fire({
      title: this.translate.instant('CALENDAR_VIEW.SWAL_DELETE_TITLE'),
      text: this.translate.instant('CALENDAR_VIEW.SWAL_DELETE_TEXT'),
      icon: 'warning',
      showCancelButton: true,
      confirmButtonColor: '#ef4444', // Red
      cancelButtonColor: '#64748b',  // Slate 500
      confirmButtonText: this.translate.instant('CALENDAR_VIEW.SWAL_DELETE_CONFIRM')
    }).then((result) => {
      if (result.isConfirmed) {
        this.calendarService.deleteCustomEvent(eventId).subscribe({
          next: (res) => {
            if (res.success) {
              this.toastr.success(this.translate.instant('CALENDAR_VIEW.MSG_EVENT_DELETED'));
              this.closeModal();
              this.fetchEvents();
            }
          },
          error: (err: any) => this.toastr.error(err.error?.message || this.translate.instant('CALENDAR_VIEW.ERR_DELETE'))
        });
      }
    });
  }
}

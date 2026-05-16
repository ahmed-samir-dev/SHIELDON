export enum EventType {
  Exam = 'Exam',
  Assignment = 'Assignment',
  Custom = 'Custom'
}

export interface CalendarEventDto {
  id: string;
  title: string;
  description?: string;
  startDate: string;
  endDate?: string;
  type: EventType;
  courseId?: string;
  courseName?: string;
  sourceEntityId?: string;
}

export interface CreateCustomEventRequest {
  title: string;
  description?: string;
  eventDate: string;
  eventEndDate?: string;
  courseId?: string;
}

export interface UpdateCustomEventRequest {
  title: string;
  description?: string;
  eventDate: string;
  eventEndDate?: string;
  courseId?: string;
}

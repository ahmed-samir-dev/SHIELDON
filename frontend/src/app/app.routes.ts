import { Routes } from '@angular/router';
import { deviceGuard } from './core/guards/device.guard';
import { authGuard, tutorGuard, adminGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  // ── Mobile Guard ──────────────────────────────────────────────────────────
  {
    path: 'mobile-blocked',
    loadComponent: () => import('./features/public/mobile-blocked/mobile-blocked').then(m => m.MobileBlocked),
    title: 'Mobile Access Restricted - SHIELDON'
  },

  // ── Public Routes (Guest) ─────────────────────────────────────────────────
  {
    path: '',
    canActivate: [deviceGuard],
    loadComponent: () => import('./layouts/public-layout/public-layout').then(m => m.PublicLayout),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/public/landing/landing').then(m => m.Landing),
        title: 'SHIELDON'
      },
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login').then(m => m.Login),
        title: 'Login - SHIELDON'
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register').then(m => m.Register),
        title: 'Register - SHIELDON'
      },
      {
        path: 'forgot-password',
        loadComponent: () => import('./features/auth/forgot-password/forgot-password').then(m => m.ForgotPassword),
        title: 'Forgot Password - SHIELDON'
      }
    ]
  },

  // ── Auth Verification Routes (Standalone) ─────────────────────────────────
  {
    path: 'auth/verify-email',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/verify-email/verify-email').then(m => m.VerifyEmail),
    title: 'Verify Email - SHIELDON'
  },
  {
    path: 'auth/reset-password',
    canActivate: [deviceGuard],
    loadComponent: () => import('./features/auth/reset-password/reset-password').then(m => m.ResetPassword),
    title: 'Reset Password - SHIELDON'
  },

  // ── Exam Engine (Distraction-Free Authenticated) ────────────────────────
  {
    path: 'exam-engine/:examId',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./features/courses/exam-engine/exam-engine').then(m => m.ExamEngine),
    title: 'Exam Engine - SHIELDON'
  },
  {
    path: 'exam-results/:attemptId',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./features/exams/exam-result-page/exam-result-page').then(m => m.ExamResultPage),
    title: 'Exam Result - SHIELDON'
  },

  // ── Authenticated Routes (Protected by authGuard) ─────────────────────────
  {
    path: '',
    canActivate: [deviceGuard, authGuard],
    loadComponent: () => import('./layouts/dashboard-layout/dashboard-layout').then(m => m.DashboardLayout),
    children: [
      { path: '', redirectTo: 'profile', pathMatch: 'full' },
      {
        path: 'profile',
        loadComponent: () => import('./features/profile/profile').then(m => m.ProfileComponent),
        title: 'My Profile - SHIELDON'
      },
      {
        path: 'courses',
        loadComponent: () => import('./features/courses/course-list/course-list').then(m => m.CourseList),
        title: 'Manage Courses - SHIELDON'
      },
      // ── Attendance ── (must come BEFORE courses/:id to avoid route conflict)
      {
        path: 'courses/:id/attendance',
        canActivate: [tutorGuard],
        loadComponent: () => import('./features/attendance/attendance-tutor/attendance-tutor').then(m => m.AttendanceTutorComponent),
        title: 'Course Attendance - SHIELDON'
      },
      {
        path: 'attendance/scan',
        loadComponent: () => import('./features/attendance/attendance-student/attendance-student').then(m => m.AttendanceStudentComponent),
        title: 'Scan Attendance - SHIELDON'
      },
      {
        path: 'admin/attendance',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/attendance/attendance-admin/attendance-admin').then(m => m.AttendanceAdminComponent),
        title: 'All Attendance Checks - SHIELDON'
      },
      
      {
        path: 'courses/:id',
        loadComponent: () => import('./features/courses/course-detail/course-detail').then(m => m.CourseDetail),
        title: 'Course Hub - SHIELDON'
      },
      {
        path: 'courses/:courseId/exams/:examId/results',
        loadComponent: () => import('./features/exams/tutor-results-panel/tutor-results-panel').then(m => m.TutorResultsPanel),
        title: 'Exam Results - SHIELDON'
      },
      {
        path: 'enrollments',
        loadComponent: () => import('./features/courses/enrollment-panel/enrollment-panel').then(m => m.EnrollmentPanel),
        title: 'Enrollment Requests - SHIELDON'
      },
      {
        path: 'reattempt-requests',
        canActivate: [tutorGuard],
        loadComponent: () => import('./features/exams/reattempt-requests/reattempt-requests').then(m => m.ReattemptRequestsComponent),
        title: 'Re-attempt Requests - SHIELDON'
      },
      {
        path: 'courses/:courseId/grades',
        canActivate: [tutorGuard],
        loadComponent: () => import('./features/grades/course-grades/course-grades').then(m => m.CourseGrades),
        title: 'Course Grades - SHIELDON'
      },
      {
        path: 'my-grades',
        loadComponent: () => import('./features/grades/my-grades/my-grades').then(m => m.MyGrades),
        title: 'My Grades - SHIELDON'
      },
      {
        path: 'monitoring/attempts/:attemptId',
        loadComponent: () => import('./features/monitoring/attempt-detail/attempt-detail').then(m => m.AttemptDetailComponent),
        title: 'Attempt Timeline - SHIELDON'
      },
      {
        path: 'chat',
        loadComponent: () => import('./features/chat/chat-messenger/chat-messenger').then(m => m.ChatMessengerComponent),
        title: 'Messages - SHIELDON'
      },
      {
        path: 'calendar',
        loadComponent: () => import('./features/calendar/calendar-view/calendar-view.component').then(m => m.CalendarViewComponent),
        title: 'Calendar & Schedule - SHIELDON'
      },
      {
        path: 'admin/dashboard',
        canActivate: [adminGuard],
        loadComponent: () => import('./features/monitoring/admin-dashboard/admin-dashboard').then(m => m.AdminDashboardComponent),
        title: 'Admin Dashboard - SHIELDON'
      },
      {
        path: 'payment-hub',
        loadComponent: () => import('./features/payment/payment-hub/payment-hub').then(m => m.PaymentHubComponent),
        title: 'Payment Hub - SHIELDON'
      },
      {
        path: 'payment/success',
        loadComponent: () => import('./features/payment/payment-success/payment-success').then(m => m.PaymentSuccessComponent),
        title: 'Payment Successful - SHIELDON'
      },
      {
        path: 'payment/cancel',
        loadComponent: () => import('./features/payment/payment-cancel/payment-cancel').then(m => m.PaymentCancelComponent),
        title: 'Payment Cancelled - SHIELDON'
      },
      { path: 'student/dashboard', redirectTo: 'courses' },
      {
        path: 'tutor/dashboard',
        canActivate: [tutorGuard],
        loadComponent: () => import('./features/monitoring/tutor-dashboard/tutor-dashboard').then(m => m.TutorDashboardComponent),
        title: 'Tutor Dashboard - SHIELDON'
      }
    ]
  },

  // ── Fallback ───────────────────────────────────────────────────────────────
  { path: '**', redirectTo: '' }
];

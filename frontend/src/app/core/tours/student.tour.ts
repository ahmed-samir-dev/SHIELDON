import Shepherd, { StepOptions } from 'shepherd.js';

export function getStudentTourSteps(): StepOptions[] {
  return [
    {
      id: 'student-welcome',
      title: 'Welcome to SHIELDON!',
      text: 'Let\'s take a quick tour of your Student Dashboard. We\'ll show you how to access your courses and take exams.',
      buttons: [
        { text: 'Skip Tour', action: () => Shepherd.activeTour?.cancel(), classes: 'shepherd-button-secondary' },
        { text: 'Start Tour', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-sidebar',
      title: 'Navigation Menu',
      text: 'This sidebar is your main navigation. Access your courses, grades, and profile from here.',
      attachTo: { element: '#nav-sidebar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-courses',
      title: 'Courses',
      text: 'Browse available courses, send enrollment requests, and access course materials and exams for the ones you are enrolled in.',
      attachTo: { element: '#nav-courses', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-grades',
      title: 'My Grades',
      text: 'Track your performance across all exams and assignments.',
      attachTo: { element: '#nav-my-grades', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-enrollments',
      title: 'Enrollments',
      text: 'Track the status of your course enrollment requests.',
      attachTo: { element: '#nav-enrollments', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-attendance',
      title: 'Attendance',
      text: 'Quickly scan QR codes to mark your presence in live lectures.',
      attachTo: { element: '#nav-attendance', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-payments',
      title: 'Payments',
      text: 'Pay your pending course fees securely.',
      attachTo: { element: '#nav-payment-hub', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-calendar',
      title: 'Calendar',
      text: 'Check your upcoming deadlines and class schedules.',
      attachTo: { element: '#nav-calendar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-chat',
      title: 'Messages',
      text: 'Message your tutors or peers directly.',
      attachTo: { element: '#nav-chat', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-ai',
      title: 'SHIELDON AI Assistant',
      text: 'Got questions? Use the AI assistant to help you understand difficult concepts or summarize materials.',
      attachTo: { element: '#nav-ai-btn', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-notifications',
      title: 'Notification Center',
      text: 'Check here for important alerts, grading updates, and new messages.',
      attachTo: { element: '.bell-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-theme',
      title: 'Theme Toggle',
      text: 'Switch between light and dark mode for a comfortable viewing experience.',
      attachTo: { element: '.theme-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-language',
      title: 'Language Toggle',
      text: 'Change the interface language seamlessly between English and Arabic.',
      attachTo: { element: '.language-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'student-profile',
      title: 'Your Profile',
      text: 'Manage your personal settings, update your profile picture, and change your password here. Good luck with your studies!',
      attachTo: { element: '#nav-profile', on: 'right' },
      buttons: [
        { text: 'Finish', action: () => Shepherd.activeTour?.complete(), classes: 'shepherd-button-primary' }
      ]
    }
  ];
}

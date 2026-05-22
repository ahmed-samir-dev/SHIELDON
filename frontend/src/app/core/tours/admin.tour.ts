import Shepherd, { StepOptions } from 'shepherd.js';

export function getAdminTourSteps(): StepOptions[] {
  return [
    {
      id: 'admin-welcome',
      title: 'Welcome to SHIELDON!',
      text: 'Let\'s take a quick tour of your new Admin Dashboard. We\'ll show you where to find everything you need to manage the system.',
      buttons: [
        { text: 'Skip Tour', action: () => Shepherd.activeTour?.cancel(), classes: 'shepherd-button-secondary' },
        { text: 'Start Tour', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-sidebar',
      title: 'Navigation Menu',
      text: 'This sidebar is your main control center. You can access all modules from here.',
      attachTo: { element: '#nav-sidebar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-dashboard',
      title: 'System Analytics',
      text: 'Get a bird\'s-eye view of system health, active users, and recent activity across the entire platform.',
      attachTo: { element: '#nav-admin-dashboard', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-courses',
      title: 'Courses',
      text: 'Create and manage courses, assign tutors, and oversee the curriculum.',
      attachTo: { element: '#nav-courses', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-enrollments',
      title: 'Enrollments',
      text: 'Review and approve pending student enrollments for courses.',
      attachTo: { element: '#nav-enrollments', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-reattempt',
      title: 'Re-attempts',
      text: 'Manage and approve exam re-attempt requests from students who faced technical issues.',
      attachTo: { element: '#nav-reattempt', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-attendance',
      title: 'Attendance',
      text: 'View and manage live attendance checks across all active courses.',
      attachTo: { element: '#nav-attendance', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-payments',
      title: 'Payments',
      text: 'Review and search student payment transactions securely.',
      attachTo: { element: '#nav-payment-hub', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-calendar',
      title: 'Calendar',
      text: 'View system-wide schedules, exams, and upcoming events.',
      attachTo: { element: '#nav-calendar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-chat',
      title: 'Messages',
      text: 'Communicate securely with tutors and students across the platform.',
      attachTo: { element: '#nav-chat', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-ai',
      title: 'SHIELDON AI Assistant',
      text: 'Need help or a quick summary? Chat with our AI assistant anytime without leaving your workflow.',
      attachTo: { element: '#nav-ai-btn', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-notifications',
      title: 'Notification Center',
      text: 'Stay updated with important system alerts, enrollment requests, and messages by clicking the bell icon.',
      attachTo: { element: '.bell-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-theme',
      title: 'Theme Toggle',
      text: 'Switch between light and dark mode for a comfortable viewing experience.',
      attachTo: { element: '.theme-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-language',
      title: 'Language Toggle',
      text: 'Change the interface language seamlessly between English and Arabic.',
      attachTo: { element: '.language-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'admin-profile',
      title: 'Your Profile',
      text: 'Manage your personal settings, update your profile picture, and change your password here. That\'s it for the tour!',
      attachTo: { element: '#nav-profile', on: 'right' },
      buttons: [
        { text: 'Finish', action: () => Shepherd.activeTour?.complete(), classes: 'shepherd-button-primary' }
      ]
    }
  ];
}

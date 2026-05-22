import Shepherd, { StepOptions } from 'shepherd.js';

export function getTutorTourSteps(): StepOptions[] {
  return [
    {
      id: 'tutor-welcome',
      title: 'Welcome to SHIELDON!',
      text: 'Let\'s take a quick tour of your Tutor Dashboard. We\'ll show you how to manage your courses and exams.',
      buttons: [
        { text: 'Skip Tour', action: () => Shepherd.activeTour?.cancel(), classes: 'shepherd-button-secondary' },
        { text: 'Start Tour', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-sidebar',
      title: 'Navigation Menu',
      text: 'This sidebar is where you can access all your teaching tools and modules.',
      attachTo: { element: '#nav-sidebar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-courses',
      title: 'Courses',
      text: 'Manage the courses you teach. Add study materials, create exams, and manage question banks here.',
      attachTo: { element: '#nav-courses', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-enrollments',
      title: 'Enrollments',
      text: 'Manage and review student enrollment requests for your courses here.',
      attachTo: { element: '#nav-enrollments', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-reattempt',
      title: 'Re-attempts',
      text: 'Review requests from your students to retake exams if they faced issues.',
      attachTo: { element: '#nav-reattempt', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-analytics',
      title: 'Tutor Dashboard',
      text: 'Track student performance, cheating attempts, and overall exam statistics.',
      attachTo: { element: '#nav-exam-analytics', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-calendar',
      title: 'Calendar',
      text: 'View your schedule and exam timings.',
      attachTo: { element: '#nav-calendar', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-chat',
      title: 'Messages',
      text: 'Message your students or system admins easily.',
      attachTo: { element: '#nav-chat', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-ai',
      title: 'SHIELDON AI Assistant',
      text: 'Need to generate exam questions or get a quick summary? Our AI assistant is here to help you speed up your workflow.',
      attachTo: { element: '#nav-ai-btn', on: 'right' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-notifications',
      title: 'Notification Center',
      text: 'Stay updated with important system alerts, enrollment requests, and new messages from students.',
      attachTo: { element: '.bell-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-theme',
      title: 'Theme Toggle',
      text: 'Switch between light and dark mode for a comfortable viewing experience.',
      attachTo: { element: '.theme-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-language',
      title: 'Language Toggle',
      text: 'Change the interface language seamlessly between English and Arabic.',
      attachTo: { element: '.language-toggle-btn', on: 'bottom' },
      buttons: [
        { text: 'Back', action: () => Shepherd.activeTour?.back(), classes: 'shepherd-button-secondary' },
        { text: 'Next', action: () => Shepherd.activeTour?.next(), classes: 'shepherd-button-primary' }
      ]
    },
    {
      id: 'tutor-profile',
      title: 'Your Profile',
      text: 'Manage your personal settings, update your profile picture, and change your password here. That\'s it for the tour!',
      attachTo: { element: '#nav-profile', on: 'right' },
      buttons: [
        { text: 'Finish', action: () => Shepherd.activeTour?.complete(), classes: 'shepherd-button-primary' }
      ]
    }
  ];
}

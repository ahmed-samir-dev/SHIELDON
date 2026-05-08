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
      title: 'Courses Hub',
      text: 'Browse available courses, send enrollment requests, and access course materials and exams for the ones you are enrolled in.',
      attachTo: { element: '#nav-courses', on: 'right' },
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

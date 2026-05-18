import { Component, OnInit, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import confetti from 'canvas-confetti';

@Component({
  selector: 'app-payment-success',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './payment-success.html',
  styleUrl: './payment-success.scss'
})
export class PaymentSuccessComponent implements OnInit {
  constructor(private router: Router, private el: ElementRef) {}

  ngOnInit(): void {
    // Small delay to allow the page to render, then fire confetti centered on this component
    setTimeout(() => this.triggerConfetti(), 300);
  }

  goToDashboard(): void {
    this.router.navigate(['/courses']);
  }

  triggerConfetti() {
    // Calculate the center of this component's view (excludes sidebar)
    const rect = this.el.nativeElement.getBoundingClientRect();
    const originX = (rect.left + rect.width / 2) / window.innerWidth;
    const originY = (rect.top + rect.height / 2) / window.innerHeight;

    const count = 250;
    const defaults = { origin: { x: originX, y: originY } };

    const fire = (particleRatio: number, opts: any) => {
      confetti({
        ...defaults,
        ...opts,
        particleCount: Math.floor(count * particleRatio)
      });
    };

    fire(0.25, { spread: 26, startVelocity: 55 });
    fire(0.2,  { spread: 60 });
    fire(0.35, { spread: 100, decay: 0.91, scalar: 0.8 });
    fire(0.1,  { spread: 120, startVelocity: 25, decay: 0.92, scalar: 1.2 });
    fire(0.1,  { spread: 120, startVelocity: 45 });
  }
}

import { Component, AfterViewInit, ElementRef, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { SeoService } from '../../../core/services/seo.service';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, TranslateModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss'
})
export class Landing implements OnInit, AfterViewInit {
  private elementRef = inject(ElementRef);
  private seoService = inject(SeoService);

  ngOnInit() {
    this.seoService.updateSeoData({
      title: 'SHIELDON',
      description: 'SHIELDON is a premium hybrid Learning Management System featuring an integrated, zero-download Anti-Cheating Engine for absolute academic integrity, proctored exams, and automated grading.',
      keywords: 'LMS, Anti-Cheat, Exam Engine, Online Proctoring, Academic Integrity, E-Learning, Automated Grading, Distance Learning, EdTech, Hybrid Learning'
    });
  }

  ngAfterViewInit() {
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('reveal-visible');
          // Optional: Stop observing once revealed
          // observer.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.1,
      rootMargin: '0px 0px -50px 0px'
    });

    const hiddenElements = this.elementRef.nativeElement.querySelectorAll('.reveal-hidden');
    hiddenElements.forEach((el: Element) => observer.observe(el));
  }
}

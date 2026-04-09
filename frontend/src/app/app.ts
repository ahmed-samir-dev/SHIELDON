import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { GlobalProgressBar } from './shared/components/global-progress-bar/global-progress-bar';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, GlobalProgressBar],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
}

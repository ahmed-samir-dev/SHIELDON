import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LucideAngularModule, AlertTriangle, Eye, ShieldAlert } from 'lucide-angular';
import { AntiCheatService } from '../anti-cheat.service';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-anti-cheat-overlay',
  standalone: true,
  imports: [CommonModule, LucideAngularModule, TranslateModule],
  templateUrl: './anti-cheat-overlay.html',
  styleUrls: ['./anti-cheat-overlay.scss']
})
export class AntiCheatOverlayComponent {
  public antiCheat = inject(AntiCheatService);

  AlertTriangle = AlertTriangle;
  Eye = Eye;
  ShieldAlert = ShieldAlert;

  // We expose strikeLevel signal directly to the template via the injected service
}

import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { ThemeService } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import { TranslateModule } from '@ngx-translate/core';
import { LucideAngularModule, Mail, Github, Linkedin, Heart, Code } from 'lucide-angular';

@Component({
  selector: 'app-public-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, TranslateModule, LucideAngularModule],
  templateUrl: './public-layout.html',
  styleUrl: './public-layout.scss'
})
export class PublicLayout {
  themeService = inject(ThemeService);
  languageService = inject(LanguageService);

  readonly Mail = Mail;
  readonly Github = Github;
  readonly Linkedin = Linkedin;
  readonly Heart = Heart;
  readonly Code = Code;
}

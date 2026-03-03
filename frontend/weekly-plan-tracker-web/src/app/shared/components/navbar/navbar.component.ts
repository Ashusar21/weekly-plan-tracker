import { Component, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss'],
})
export class NavbarComponent {
  member = computed(() => this.auth.currentMember());
  isDark = computed(() => this.theme.isDark());

  constructor(
    private auth: AuthService,
    private theme: ThemeService,
    public router: Router,
  ) {}

  switchUser(): void {
    this.auth.logout();
    this.router.navigate(['/identity']);
  }
  goHome(): void {
    this.router.navigate(['/hub']);
  }
  toggleTheme(): void {
    this.theme.toggle();
  }
}

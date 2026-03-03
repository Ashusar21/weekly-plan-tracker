import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ThemeService } from './core/services/theme.service';
import { AuthService } from './core/services/auth.service';
import { Router } from '@angular/router';
import { TeamMemberService } from './core/services/team-member.service';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    CommonModule,
    NavbarComponent,
    FooterComponent,
    ToastComponent,
  ],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  constructor(
    private theme: ThemeService,
    private auth: AuthService,
    private router: Router,
    private teamService: TeamMemberService,
  ) {}

  ngOnInit(): void {
    // theme is applied in ThemeService constructor, nothing needed here
    this.teamService.getAll().subscribe({
      next: (members) => {
        if (!members || members.length === 0) this.router.navigate(['/setup']);
        else if (!this.auth.currentMember())
          this.router.navigate(['/identity']);
        else this.router.navigate(['/hub']);
      },
      error: () => this.router.navigate(['/setup']),
    });
  }
}

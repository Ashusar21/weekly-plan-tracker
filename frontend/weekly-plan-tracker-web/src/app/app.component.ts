import { Component, OnInit } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { ThemeService } from './core/services/theme.service';
import { AuthService } from './core/services/auth.service';
import { TeamMemberService } from './core/services/team-member.service';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { ToastComponent } from './shared/components/toast/toast.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent, ToastComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
})
export class AppComponent implements OnInit {
  showChrome = false;

  constructor(
    private theme: ThemeService,
    private auth: AuthService,
    private router: Router,
    private teamService: TeamMemberService,
  ) {}

  ngOnInit(): void {
    // Update chrome visibility on every route change
    this.router.events.pipe(
      filter(e => e instanceof NavigationEnd)
    ).subscribe((e: any) => {
      const url = e.urlAfterRedirects || e.url;
      this.showChrome = !url.startsWith('/setup');
    });

    // Initial redirect
    this.teamService.anyExists().subscribe({
      next: ({ exists }) => {
        if (!exists) {
          this.showChrome = false;
          this.router.navigate(['/setup']);
        } else if (!this.auth.currentMember()) {
          this.showChrome = true;
          this.router.navigate(['/identity']);
        } else {
          this.showChrome = true;
          this.router.navigate(['/hub']);
        }
      },
      error: () => {
        this.showChrome = false;
        this.router.navigate(['/setup']);
      },
    });
  }
}

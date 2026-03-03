import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';
import { TeamMemberService } from '../../../core/services/team-member.service';
import { BacklogService } from '../../../core/services/backlog.service';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
})
export class FooterComponent {
  constructor(
    private toast: ToastService,
    private teamService: TeamMemberService,
    private backlogService: BacklogService,
  ) {}

  downloadData(): void {
    const data: Record<string, string> = {};
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i)!;
      data[key] = localStorage.getItem(key)!;
    }
    const blob = new Blob([JSON.stringify(data, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'weekly-plan-backup.json';
    a.click();
    URL.revokeObjectURL(url);
    this.toast.show('Your data was saved to a file.');
  }

  loadData(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const data = JSON.parse(e.target?.result as string);
        Object.entries(data).forEach(([k, v]) =>
          localStorage.setItem(k, v as string),
        );
        this.toast.show('Data loaded! Refreshing...');
        setTimeout(() => window.location.reload(), 1000);
      } catch {
        this.toast.show('Invalid file format', 'error');
      }
    };
    reader.readAsText(file);
  }

  seedData(): void {
    if (
      !confirm(
        'This will add sample team members and backlog items. Are you sure?',
      )
    )
      return;
    const members = [
      { name: 'Alice Chen' },
      { name: 'Bob Martinez' },
      { name: 'Carol Singh' },
      { name: 'Dave Kim' },
    ];
    const backlog = [
      {
        title: 'Customer onboarding redesign',
        description: 'Revamp the onboarding flow.',
        category: 'ClientFocused' as const,
        estimatedEffort: 12,
      },
      {
        title: 'Fix billing invoice formatting',
        description: 'Some invoices show wrong currency format.',
        category: 'ClientFocused' as const,
        estimatedEffort: 4,
      },
      {
        title: 'Customer feedback dashboard',
        description: 'Build a dashboard showing NPS scores.',
        category: 'ClientFocused' as const,
        estimatedEffort: 16,
      },
      {
        title: 'Migrate database to PostgreSQL 16',
        description: 'Upgrade from PG 14 to PG 16.',
        category: 'TechDebt' as const,
        estimatedEffort: 20,
      },
      {
        title: 'Remove deprecated API endpoints',
        description: 'Clean up v1 API routes.',
        category: 'TechDebt' as const,
        estimatedEffort: 8,
      },
      {
        title: 'Add unit tests for payment module',
        description: 'Coverage is below 50%.',
        category: 'TechDebt' as const,
        estimatedEffort: 10,
      },
      {
        title: 'Experiment with LLM-based search',
        description: 'Prototype semantic search using embeddings.',
        category: 'RAndD' as const,
        estimatedEffort: 15,
      },
      {
        title: 'Evaluate new caching strategy',
        description: 'Compare Redis Cluster vs Memcached.',
        category: 'RAndD' as const,
        estimatedEffort: 6,
      },
      {
        title: 'Build internal CLI tool',
        description: 'A command-line tool for common dev tasks.',
        category: 'RAndD' as const,
        estimatedEffort: 8,
      },
      {
        title: 'Client SSO integration',
        description: 'Support SAML-based SSO for enterprise clients.',
        category: 'ClientFocused' as const,
        estimatedEffort: 18,
      },
    ];

    const memberCalls = members.map((m) => this.teamService.create(m));
    Promise.all(memberCalls.map((c) => c.toPromise()))
      .then(() => {
        const backlogCalls = backlog.map((b) => this.backlogService.create(b));
        Promise.all(backlogCalls.map((c) => c.toPromise())).then(() => {
          this.toast.show('Sample data loaded! Pick a person to get started.');
          setTimeout(() => window.location.reload(), 1000);
        });
      })
      .catch(() => this.toast.show('Failed to seed data', 'error'));
  }

  resetApp(): void {
    if (confirm('Reset everything? This cannot be undone.')) {
      localStorage.clear();
      window.location.href = '/setup';
    }
  }
}

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BacklogService } from '../../../core/services/backlog.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';
import {
  BacklogItem,
  Category,
  BacklogItemStatus,
} from '../../../core/models/backlog-item.model';

@Component({
  selector: 'app-backlog-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './backlog-list.component.html',
  styleUrls: ['./backlog-list.component.scss'],
})
export class BacklogListComponent implements OnInit {
  items: BacklogItem[] = [];
  loading = true;
  filterStatus: BacklogItemStatus | '' = 'Available';
  filterCategory: Category | '' = '';
  confirmModal: {
    show: boolean;
    title: string;
    message: string;
    action: () => void;
  } = { show: false, title: '', message: '', action: () => {} };

  constructor(
    private backlog: BacklogService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.backlog
      .getAll(this.filterStatus || undefined, this.filterCategory || undefined)
      .subscribe({
        next: (items) => {
          this.items = items;
          this.loading = false;
        },
        error: () => {
          this.toast.show('Failed to load backlog', 'error');
          this.loading = false;
        },
      });
  }

  archive(item: BacklogItem): void {
    this.confirmModal = {
      show: true,
      title: 'Archive Item',
      message: `Archive "${item.title}"? It won't appear in future planning.`,
      action: () => {
        this.backlog.archive(item.id).subscribe({
          next: () => {
            this.toast.show('Item archived!');
            this.confirmModal.show = false;
            this.load();
          },
          error: () => {
            this.toast.show('Failed to archive', 'error');
            this.confirmModal.show = false;
          },
        });
      },
    };
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }
}

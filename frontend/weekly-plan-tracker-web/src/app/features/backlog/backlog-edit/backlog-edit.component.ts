import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BacklogService } from '../../../core/services/backlog.service';
import { ToastService } from '../../../core/services/toast.service';
import { Category } from '../../../core/models/backlog-item.model';

@Component({
  selector: 'app-backlog-edit',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './backlog-edit.component.html',
  styleUrls: ['./backlog-edit.component.scss'],
})
export class BacklogEditComponent implements OnInit {
  isEdit = false;
  itemId = '';
  loading = true;
  saving = false;

  form = {
    title: '',
    description: '',
    category: 'ClientFocused' as Category,
    estimatedEffort: undefined as number | undefined,
  };

  constructor(
    private backlog: BacklogService,
    private toast: ToastService,
    private route: ActivatedRoute,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.itemId = this.route.snapshot.paramMap.get('id') ?? '';
    this.isEdit = !!this.itemId;
    if (this.isEdit) {
      this.backlog.getById(this.itemId).subscribe({
        next: (item) => {
          this.form = {
            title: item.title,
            description: item.description,
            category: item.category,
            estimatedEffort: item.estimatedEffort,
          };
          this.loading = false;
        },
        error: () => {
          this.toast.show('Failed to load item', 'error');
          this.router.navigate(['/backlog']);
        },
      });
    } else {
      this.loading = false;
    }
  }

  save(): void {
    if (!this.form.title.trim()) {
      this.toast.show('Title is required', 'error');
      return;
    }
    this.saving = true;
    const dto = {
      title: this.form.title.trim(),
      description: this.form.description.trim(),
      category: this.form.category,
      estimatedEffort: this.form.estimatedEffort,
    };
    const call = this.isEdit
      ? this.backlog.update(this.itemId, dto)
      : this.backlog.create(dto);
    call.subscribe({
      next: () => {
        this.toast.show(this.isEdit ? 'Item updated!' : 'Item created!');
        this.router.navigate(['/backlog']);
      },
      error: () => {
        this.toast.show('Failed to save', 'error');
        this.saving = false;
      },
    });
  }
}

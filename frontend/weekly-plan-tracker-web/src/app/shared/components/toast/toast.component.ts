import { Component, computed } from '@angular/core';

import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [],
  templateUrl: './toast.component.html',
  styleUrls: ['./toast.component.scss']
})
export class ToastComponent {
  toasts = computed(() => this.toastService.toasts());
  constructor(private toastService: ToastService) {}
  dismiss(id: number): void { this.toastService.dismiss(id); }
}

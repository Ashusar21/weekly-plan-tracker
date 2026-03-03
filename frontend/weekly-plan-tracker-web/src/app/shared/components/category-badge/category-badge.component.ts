import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Category } from '../../../core/models/backlog-item.model';

@Component({
  selector: 'app-category-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-badge.component.html',
  styleUrls: ['./category-badge.component.scss'],
})
export class CategoryBadgeComponent {
  @Input() category!: Category;

  get cssClass(): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[this.category] ?? '';
  }

  get label(): string {
    const map: Record<string, string> = {
      ClientFocused: 'Client Focused',
      TechDebt: 'Tech Debt',
      RAndD: 'R&D',
    };
    return map[this.category] ?? this.category;
  }
}

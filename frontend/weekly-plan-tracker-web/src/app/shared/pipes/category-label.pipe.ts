import { Pipe, PipeTransform } from '@angular/core';
import { Category } from '../../core/models/backlog-item.model';

@Pipe({ name: 'categoryLabel', standalone: true })
export class CategoryLabelPipe implements PipeTransform {
  transform(value: Category): string {
    const map: Record<string, string> = {
      ClientFocused: 'Client Focused',
      TechDebt: 'Tech Debt',
      RAndD: 'R&D',
    };
    return map[value] ?? value;
  }
}

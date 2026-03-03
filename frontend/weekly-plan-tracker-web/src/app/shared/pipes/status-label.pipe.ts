import { Pipe, PipeTransform } from '@angular/core';
import { ProgressStatus } from '../../core/models/member-plan.model';

@Pipe({ name: 'statusLabel', standalone: true })
export class StatusLabelPipe implements PipeTransform {
  transform(value: ProgressStatus): string {
    const map: Record<string, string> = {
      NotStarted: 'Not Started',
      InProgress: 'In Progress',
      Completed: 'Completed',
      Blocked: 'Blocked',
    };
    return map[value] ?? value;
  }
}

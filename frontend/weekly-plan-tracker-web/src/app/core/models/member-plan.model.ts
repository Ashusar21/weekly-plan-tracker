import { Category } from './backlog-item.model';

export type ProgressStatus = 'NotStarted' | 'InProgress' | 'Completed' | 'Blocked';

export interface TaskAssignment {
  id: string;
  backlogItemId: string;
  backlogItemTitle: string;
  backlogItemDescription: string;
  category: Category;
  categoryLabel: string;
  committedHours: number;
  hoursCompleted: number;
  progressStatus: ProgressStatus;
  progressStatusLabel: string;
}

export interface MemberPlan {
  id: string;
  planningWeekId: string;
  memberId: string;
  memberName: string;
  totalPlannedHours: number;
  isReady: boolean;
  taskAssignments: TaskAssignment[];
}

export interface ClaimBacklogItemDto {
  backlogItemId: string;
  committedHours: number;
}

export interface UpdateCommittedHoursDto {
  committedHours: number;
}

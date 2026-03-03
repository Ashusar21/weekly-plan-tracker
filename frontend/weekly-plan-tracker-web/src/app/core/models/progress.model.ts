import { TaskAssignment, ProgressStatus } from './member-plan.model';

export interface ProgressUpdateDto {
  id: string;
  taskAssignmentId: string;
  updatedBy: string;
  updatedByName: string;
  previousHoursCompleted: number;
  newHoursCompleted: number;
  previousStatus: ProgressStatus;
  newStatus: ProgressStatus;
  note: string;
  timestamp: string;
}

export interface SubmitProgressUpdateDto {
  hoursCompleted: number;
  status: ProgressStatus;
  note: string;
  updatedBy: string;
}

export interface CategoryProgress {
  category: string;
  categoryLabel: string;
  totalTasks: number;
  completedTasks: number;
  committedHours: number;
  hoursCompleted: number;
  tasks: TaskAssignment[];
}

export interface MemberProgress {
  memberId: string;
  memberName: string;
  totalTasks: number;
  completedTasks: number;
  blockedTasks: number;
  committedHours: number;
  hoursCompleted: number;
  tasks: TaskAssignment[];
}

export interface TeamProgress {
  weekId: string;
  totalTasks: number;
  completedTasks: number;
  blockedTasks: number;
  inProgressTasks: number;
  totalCommittedHours: number;
  totalHoursCompleted: number;
  byCategory: CategoryProgress[];
  byMember: MemberProgress[];
}

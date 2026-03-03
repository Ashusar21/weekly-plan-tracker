export type WeekState = 'Setup' | 'Planning' | 'Frozen' | 'Completed';

export interface CategoryAllocation {
  category: string;
  categoryLabel: string;
  percentage: number;
  budgetHours: number;
}

export interface PlanningWeek {
  id: string;
  planningDate: string;
  executionStartDate: string;
  executionEndDate: string;
  state: WeekState;
  teamCapacity: number;
  createdAt: string;
  categoryAllocations: CategoryAllocation[];
  participatingMemberIds: string[];
}

export interface CreatePlanningWeekDto {
  planningDate: string;
  participatingMemberIds: string[];
  clientFocusedPercent: number;
  techDebtPercent: number;
  rAndDPercent: number;
}

export interface UpdateAllocationsDto {
  clientFocusedPercent: number;
  techDebtPercent: number;
  rAndDPercent: number;
  participatingMemberIds: string[];
}

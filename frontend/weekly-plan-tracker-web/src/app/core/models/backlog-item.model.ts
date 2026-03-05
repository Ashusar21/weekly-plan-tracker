export type Category = 'ClientFocused' | 'TechDebt' | 'RAndD';
export type BacklogItemStatus = 'Available' | 'Archived' | 'Completed';

export interface BacklogItem {
  id: string;
  title: string;
  description: string;
  category: Category;
  categoryLabel: string;
  estimatedEffort?: number;
  status: BacklogItemStatus;
  createdAt: string;
}

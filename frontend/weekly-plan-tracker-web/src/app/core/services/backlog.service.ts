import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  BacklogItem,
  BacklogItemStatus,
  Category,
} from '../models/backlog-item.model';

@Injectable({ providedIn: 'root' })
export class BacklogService {
  constructor(private api: ApiService) {}

  getAll(
    status?: BacklogItemStatus | string,
    category?: Category | string,
  ): Observable<BacklogItem[]> {
    const params: Record<string, string> = {};
    if (status) params['status'] = status;
    if (category) params['category'] = category;
    return this.api.get<BacklogItem[]>('/backlog', params);
  }

  getById(id: string): Observable<BacklogItem> {
    return this.api.get<BacklogItem>(`/backlog/${id}`);
  }

  create(dto: {
    title: string;
    description: string;
    category: Category;
    estimatedEffort?: number;
  }): Observable<BacklogItem> {
    return this.api.post<BacklogItem>('/backlog', dto);
  }

  update(
    id: string,
    dto: {
      title: string;
      description: string;
      category: Category;
      estimatedEffort?: number;
    },
  ): Observable<BacklogItem> {
    return this.api.put<BacklogItem>(`/backlog/${id}`, dto);
  }

  archive(id: string): Observable<void> {
    return this.api.patch<void>(`/backlog/${id}/archive`, {});
  }
}

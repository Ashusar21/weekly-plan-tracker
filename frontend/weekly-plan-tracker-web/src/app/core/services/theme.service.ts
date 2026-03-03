import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'wpt_theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _isDark = signal<boolean>(this.loadTheme());
  isDark = this._isDark.asReadonly();

  constructor() {
    this.applyTheme(this._isDark());
  }

  private loadTheme(): boolean {
    return localStorage.getItem(STORAGE_KEY) === 'dark';
  }

  toggle(): void {
    const next = !this._isDark();
    this._isDark.set(next);
    localStorage.setItem(STORAGE_KEY, next ? 'dark' : 'light');
    this.applyTheme(next);
  }

  apply(): void {
    this.applyTheme(this._isDark());
  }

  private applyTheme(dark: boolean): void {
    document.body.classList.toggle('theme-dark', dark);
    document.body.classList.toggle('theme-light', !dark);
  }
}

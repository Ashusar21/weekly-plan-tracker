import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './components/navbar/navbar.component';
import { FooterComponent } from './components/footer/footer.component';
import { ToastComponent } from './components/toast/toast.component';
import { ConfirmModalComponent } from './components/confirm-modal/confirm-modal.component';
import { CategoryBadgeComponent } from './components/category-badge/category-badge.component';
import { CategoryLabelPipe } from './pipes/category-label.pipe';
import { StatusLabelPipe } from './pipes/status-label.pipe';

@NgModule({
  imports: [
    CommonModule,
    NavbarComponent,
    FooterComponent,
    ToastComponent,
    ConfirmModalComponent,
    CategoryBadgeComponent,
    CategoryLabelPipe,
    StatusLabelPipe,
  ],
  exports: [
    NavbarComponent,
    FooterComponent,
    ToastComponent,
    ConfirmModalComponent,
    CategoryBadgeComponent,
    CategoryLabelPipe,
    StatusLabelPipe,
  ],
})
export class SharedModule {}

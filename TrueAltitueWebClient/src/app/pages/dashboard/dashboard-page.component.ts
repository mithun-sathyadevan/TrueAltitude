import { Component } from '@angular/core';

import { ExamsCardComponent } from '../../components/exams-card/exams-card.component';
import { ModulesCardComponent } from '../../components/modules-card/modules-card.component';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { StatCardComponent } from '../../components/stat-card/stat-card.component';
import { TopHeaderComponent } from '../../components/top-header/top-header.component';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [SidebarComponent, TopHeaderComponent, StatCardComponent, ModulesCardComponent, ExamsCardComponent],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
})
export class DashboardPageComponent {}

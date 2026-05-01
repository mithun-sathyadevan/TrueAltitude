import { Component } from '@angular/core';

interface NavItem {
  label: string;
  iconClass: string;
  active?: boolean;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent {
  protected readonly navItems: NavItem[] = [
    { label: 'Dashboard', iconClass: 'fas fa-th-large', active: true },
    { label: 'My Courses', iconClass: 'fas fa-book' },
    { label: 'Flight Schedule', iconClass: 'fas fa-calendar-alt' },
    { label: 'Assignments', iconClass: 'fas fa-file-alt' },
    { label: 'Settings', iconClass: 'fas fa-cog' },
  ];
}

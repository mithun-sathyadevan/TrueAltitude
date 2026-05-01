import { Component } from '@angular/core';

interface ModuleItem {
  label: string;
  iconClass: string;
}

@Component({
  selector: 'app-modules-card',
  standalone: true,
  templateUrl: './modules-card.component.html',
  styleUrl: './modules-card.component.scss',
})
export class ModulesCardComponent {
  protected readonly modules: ModuleItem[] = [
    { label: 'Aerodynamics 101', iconClass: 'fas fa-check-circle success' },
    { label: 'Meteorology Basics', iconClass: 'fas fa-clock pending' },
    { label: 'Radio Communications', iconClass: 'fas fa-play-circle active' },
  ];
}

import { Component } from '@angular/core';
import { NgIf } from '@angular/common';

import { LoadingService } from '../../services/loading.service';

@Component({
  selector: 'app-global-loader',
  standalone: true,
  imports: [NgIf],
  templateUrl: './global-loader.component.html',
  styleUrl: './global-loader.component.scss'
})
export class GlobalLoaderComponent {
  constructor(public readonly loadingService: LoadingService) {}
}

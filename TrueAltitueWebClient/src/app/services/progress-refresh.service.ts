import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ProgressRefreshService {
  private readonly progressUpdatedSubject = new Subject<void>();
  readonly progressUpdated$ = this.progressUpdatedSubject.asObservable();

  notifyProgressUpdated(): void {
    this.progressUpdatedSubject.next();
  }
}

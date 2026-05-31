import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { LearningPageComponent } from '../../pages/learning/learning-page.component';
import { LearningShellPageComponent } from '../../pages/learning-shell/learning-shell-page.component';
import { LearningSubjectsPageComponent } from '../../pages/learning-subjects/learning-subjects-page.component';
import { RealtimeExamPageComponent } from '../../pages/realtime-exam/realtime-exam-page.component';

export const LEARNING_ROUTES: Routes = [
  {
    path: '',
    component: LearningShellPageComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'subjects', pathMatch: 'full' },
      { path: 'subjects', component: LearningSubjectsPageComponent },
      { path: 'topics/:subjectId', component: LearningPageComponent },
      { path: 'realtime-exam', component: RealtimeExamPageComponent },
    ],
  },
];
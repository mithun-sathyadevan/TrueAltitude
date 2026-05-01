import { Component, OnDestroy } from '@angular/core';

interface ExamOption {
  id: string;
  text: string;
  isCorrect: boolean;
  explanation: string;
}

interface ExamQuestion {
  id: string;
  text: string;
  options: ExamOption[];
}

@Component({
  selector: 'app-realtime-exam-page',
  standalone: true,
  templateUrl: './realtime-exam-page.component.html',
  styleUrl: './realtime-exam-page.component.scss',
})
export class RealtimeExamPageComponent implements OnDestroy {
  protected readonly totalSeconds = 8 * 60;
  protected timeLeftSeconds = this.totalSeconds;
  protected isExamStarted = false;
  protected isExamFinished = false;
  protected activeQuestionIndex = 0;
  protected selectedAnswers: Record<string, string> = {};

  private timerHandle: ReturnType<typeof setInterval> | null = null;

  protected readonly questions: ExamQuestion[] = [
    {
      id: 'exam-q1',
      text: 'During a repetitive flight route, which item must be revalidated every dispatch cycle?',
      options: [
        {
          id: 'exam-q1-a',
          text: 'Weather minima, NOTAM constraints, and alternate suitability',
          isCorrect: true,
          explanation: 'Pass: these can change each cycle and directly impact dispatch safety.',
        },
        {
          id: 'exam-q1-b',
          text: 'Only the route nickname used by the operations team',
          isCorrect: false,
          explanation: 'Fail: route labels help internally but do not ensure flight legality.',
        },
        {
          id: 'exam-q1-c',
          text: 'Only the date format used in filing',
          isCorrect: false,
          explanation: 'Fail: formatting alone does not validate operational conditions.',
        },
      ],
    },
    {
      id: 'exam-q2',
      text: 'What is the safest action if the planned waypoint sequence conflicts with current ATC flow restrictions?',
      options: [
        {
          id: 'exam-q2-a',
          text: 'Proceed with the original route and explain later',
          isCorrect: false,
          explanation: 'Fail: route compliance must be handled before execution, not after.',
        },
        {
          id: 'exam-q2-b',
          text: 'Refile with an updated route and brief the crew on the change',
          isCorrect: true,
          explanation: 'Pass: compliant refiling and clear briefing preserve safety and legality.',
        },
        {
          id: 'exam-q2-c',
          text: 'Ignore restrictions if estimated delay is small',
          isCorrect: false,
          explanation: 'Fail: restrictions are mandatory regardless of perceived delay impact.',
        },
      ],
    },
    {
      id: 'exam-q3',
      text: 'Which metric best indicates readiness before submitting a repetitive plan?',
      options: [
        {
          id: 'exam-q3-a',
          text: 'All operational checks are green and route validation has no unresolved flags',
          isCorrect: true,
          explanation: 'Pass: clear validation status is the best pre-submission indicator.',
        },
        {
          id: 'exam-q3-b',
          text: 'The fastest possible submission time',
          isCorrect: false,
          explanation: 'Fail: speed without validation increases risk of rejection.',
        },
        {
          id: 'exam-q3-c',
          text: 'Only if previous day plan was accepted',
          isCorrect: false,
          explanation: 'Fail: each day has unique conditions and must be checked fresh.',
        },
      ],
    },
  ];

  ngOnDestroy(): void {
    this.clearTimer();
  }

  protected startExam(): void {
    this.isExamStarted = true;
    this.isExamFinished = false;
    this.activeQuestionIndex = 0;
    this.selectedAnswers = {};
    this.timeLeftSeconds = this.totalSeconds;
    this.startTimer();
  }

  protected restartExam(): void {
    this.startExam();
  }

  protected submitExam(): void {
    this.isExamFinished = true;
    this.clearTimer();
  }

  protected goToQuestion(index: number): void {
    this.activeQuestionIndex = index;
  }

  protected nextQuestion(): void {
    if (this.activeQuestionIndex < this.questions.length - 1) {
      this.activeQuestionIndex += 1;
    }
  }

  protected prevQuestion(): void {
    if (this.activeQuestionIndex > 0) {
      this.activeQuestionIndex -= 1;
    }
  }

  protected selectAnswer(questionId: string, optionId: string): void {
    if (!this.isExamFinished) {
      this.selectedAnswers = {
        ...this.selectedAnswers,
        [questionId]: optionId,
      };
    }
  }

  protected getSelectedOption(question: ExamQuestion): ExamOption | undefined {
    return question.options.find((option) => option.id === this.selectedAnswers[question.id]);
  }

  protected answeredCount(): number {
    return Object.keys(this.selectedAnswers).length;
  }

  protected scoreCount(): number {
    return this.questions.filter((question) => this.getSelectedOption(question)?.isCorrect).length;
  }

  protected passPercent(): number {
    return Math.round((this.scoreCount() / this.questions.length) * 100);
  }

  protected hasPassed(): boolean {
    return this.passPercent() >= 70;
  }

  protected formattedTimeLeft(): string {
    const minutes = Math.floor(this.timeLeftSeconds / 60)
      .toString()
      .padStart(2, '0');
    const seconds = (this.timeLeftSeconds % 60).toString().padStart(2, '0');
    return `${minutes}:${seconds}`;
  }

  private startTimer(): void {
    this.clearTimer();

    this.timerHandle = setInterval(() => {
      this.timeLeftSeconds -= 1;

      if (this.timeLeftSeconds <= 0) {
        this.timeLeftSeconds = 0;
        this.submitExam();
      }
    }, 1000);
  }

  private clearTimer(): void {
    if (this.timerHandle) {
      clearInterval(this.timerHandle);
      this.timerHandle = null;
    }
  }
}

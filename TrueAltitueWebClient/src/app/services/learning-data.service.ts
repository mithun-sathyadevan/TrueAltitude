import { Injectable } from '@angular/core';

import { TopicNode } from '../models/learning.models';

@Injectable({ providedIn: 'root' })
export class LearningDataService {
  private readonly baseSubjects: TopicNode[] = [
    {
      id: 'subject-navigation',
      title: 'Navigation',
      description: 'Core navigation knowledge for flight planning and execution.',
      children: [
        {
          id: 'topic-repetitive-flight-plan',
          title: 'Repetitive Flight Plan',
          description: 'Build and file recurring routes accurately with valid timing and alternates.',
          questions: [
            {
              id: 'q-rfp-1',
              text: 'What is the biggest advantage of using a Repetitive Flight Plan for scheduled sectors?',
              options: [
                {
                  id: 'q-rfp-1-a',
                  text: 'It removes the need for weather checks.',
                  isCorrect: false,
                  explanation: 'Fail: weather and NOTAM review are always mandatory before dispatch.',
                },
                {
                  id: 'q-rfp-1-b',
                  text: 'It reduces filing errors and dispatch time for recurring routes.',
                  isCorrect: true,
                  explanation:
                    'Pass: repetitive templates improve consistency and reduce data-entry mistakes.',
                },
                {
                  id: 'q-rfp-1-c',
                  text: 'It allows pilots to skip route briefings.',
                  isCorrect: false,
                  explanation: 'Fail: briefing remains mandatory even for repeated routes.',
                },
              ],
            },
            {
              id: 'q-rfp-2',
              text: 'Before reusing an RFP template, what should be validated first?',
              options: [
                {
                  id: 'q-rfp-2-a',
                  text: 'Route validity, slot constraints, and alternate availability.',
                  isCorrect: true,
                  explanation:
                    'Pass: these checks ensure the template is still compliant and operationally safe.',
                },
                {
                  id: 'q-rfp-2-b',
                  text: 'Only the flight number and pilot name.',
                  isCorrect: false,
                  explanation: 'Fail: operational constraints matter more than crew naming details.',
                },
                {
                  id: 'q-rfp-2-c',
                  text: 'Only the aircraft paint scheme and tail logo.',
                  isCorrect: false,
                  explanation: 'Fail: cosmetic details do not affect filing validity.',
                },
              ],
            },
          ],
          videos: [
            {
              id: 'vid-rfp-1',
              title: 'RFP Setup for Daily Sectors',
              duration: '14:20',
              summary: 'Template creation flow, validation checks, and dispatch handoff best practices.',
            },
            {
              id: 'vid-rfp-2',
              title: 'Error Recovery in Repetitive Filing',
              duration: '09:45',
              summary: 'How to handle route rejection and quickly publish a corrected plan.',
            },
          ],
          blogs: [
            {
              id: 'blog-rfp-1',
              chapter: 'Chapter 1',
              title: 'RFP Fundamentals and Common Mistakes',
              readMinutes: 7,
              summary: 'Top operational mistakes and a practical pre-filing checklist.',
            },
            {
              id: 'blog-rfp-2',
              chapter: 'Chapter 2',
              title: 'Maintaining Repetitive Route Libraries',
              readMinutes: 6,
              summary: 'Versioning strategy for route templates across fleets and seasons.',
            },
          ],
        },
        {
          id: 'topic-nav-subject',
          title: 'Navigation as Subject',
          description: 'Theory and procedures for enroute awareness and position management.',
          children: [
            {
              id: 'topic-nav-waypoint-management',
              title: 'Waypoint Management',
              description: 'Topic under Navigation as Subject for deeper hierarchy support.',
            },
          ],
          videos: [
            {
              id: 'vid-nav-1',
              title: 'Map Reading for IFR Cadets',
              duration: '11:30',
              summary: 'Symbol interpretation, airway reading, and route verification.',
            },
          ],
          blogs: [
            {
              id: 'blog-nav-1',
              chapter: 'Chapter 3',
              title: 'How to Build Strong Positional Awareness',
              readMinutes: 8,
              summary: 'Practical techniques to avoid drift and maintain route confidence.',
            },
          ],
        },
      ],
    },
    {
      id: 'subject-radio-navigation',
      title: 'Radio Navigation',
      description: 'Procedures, systems, and interpretation for radio-based navigation.',
      children: [
        {
          id: 'topic-rnav-chapterwise',
          title: 'Chapterwise Questions',
          description: 'Pick a chapter below and attempt chapter-specific MCQ sets.',
          children: [
            {
              id: 'topic-rnav-ch01-vor-basics',
              title: 'Chapter 01 - VOR Fundamentals',
              description: 'Core VOR station interpretation and TO/FROM logic.',
              questions: [
                {
                  id: 'q-rnav-ch01-1',
                  text: 'Which indication confirms you are tracking inbound to a VOR station?',
                  options: [
                    {
                      id: 'q-rnav-ch01-1-a',
                      text: 'TO indication with centered CDI after proper radial intercept.',
                      isCorrect: true,
                      explanation: 'Pass: inbound tracking requires TO sense and corrected CDI alignment.',
                    },
                    {
                      id: 'q-rnav-ch01-1-b',
                      text: 'FROM indication with no CDI monitoring.',
                      isCorrect: false,
                      explanation: 'Fail: FROM indicates outbound sense and cannot confirm inbound tracking.',
                    },
                    {
                      id: 'q-rnav-ch01-1-c',
                      text: 'Only DME distance trend without CDI reference.',
                      isCorrect: false,
                      explanation: 'Fail: DME helps range awareness, not course centering logic.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch02-radial-intercept',
              title: 'Chapter 02 - Radial Intercept',
              description: 'Intercept planning and angle selection for stable capture.',
              questions: [
                {
                  id: 'q-rnav-ch02-1',
                  text: 'What intercept angle is usually safer for controlled radial capture?',
                  options: [
                    {
                      id: 'q-rnav-ch02-1-a',
                      text: 'Moderate intercept with correction for wind drift.',
                      isCorrect: true,
                      explanation: 'Pass: moderate intercept reduces overshoot risk and improves stabilization.',
                    },
                    {
                      id: 'q-rnav-ch02-1-b',
                      text: 'Maximum intercept angle regardless of wind.',
                      isCorrect: false,
                      explanation: 'Fail: aggressive intercepts often overshoot the radial.',
                    },
                    {
                      id: 'q-rnav-ch02-1-c',
                      text: 'No intercept plan; wait for CDI to center naturally.',
                      isCorrect: false,
                      explanation: 'Fail: structured intercept planning is essential for predictability.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch03-dme-arc',
              title: 'Chapter 03 - DME Arc Procedures',
              description: 'Maintaining constant range and correcting radial drift on arc segments.',
              questions: [
                {
                  id: 'q-rnav-ch03-1',
                  text: 'During a DME arc, what should be monitored continuously?',
                  options: [
                    {
                      id: 'q-rnav-ch03-1-a',
                      text: 'DME range trend and radial progression together.',
                      isCorrect: true,
                      explanation: 'Pass: both are needed to stay on arc and maintain situational awareness.',
                    },
                    {
                      id: 'q-rnav-ch03-1-b',
                      text: 'Only heading without distance checks.',
                      isCorrect: false,
                      explanation: 'Fail: heading alone cannot guarantee arc distance accuracy.',
                    },
                    {
                      id: 'q-rnav-ch03-1-c',
                      text: 'Only estimated time between fixes.',
                      isCorrect: false,
                      explanation: 'Fail: timing helps planning but does not ensure arc fidelity.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch04-adf-bearing',
              title: 'Chapter 04 - ADF Relative Bearing',
              description: 'Interpreting relative bearing and deriving track corrections.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch04-1',
                  text: 'Relative bearing from ADF mainly represents:',
                  options: [
                    {
                      id: 'q-rnav-ch04-1-a',
                      text: 'Direction to station relative to aircraft nose.',
                      isCorrect: true,
                      explanation: 'Pass: ADF needle gives relative direction, not direct wind correction.',
                    },
                    {
                      id: 'q-rnav-ch04-1-b',
                      text: 'Automatic drift correction command.',
                      isCorrect: false,
                      explanation: 'Fail: pilot computes correction; ADF does not command correction.',
                    },
                    {
                      id: 'q-rnav-ch04-1-c',
                      text: 'Groundspeed over NDB.',
                      isCorrect: false,
                      explanation: 'Fail: groundspeed is not provided by bearing needle alone.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch05-holding-rnav',
              title: 'Chapter 05 - Holding With Navaids',
              description: 'Entry procedures and timing corrections in navaid-based holding.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch05-1',
                  text: 'In hold execution, why is outbound timing adjustment important?',
                  options: [
                    {
                      id: 'q-rnav-ch05-1-a',
                      text: 'It keeps inbound leg duration within procedural target.',
                      isCorrect: true,
                      explanation: 'Pass: outbound adjustments compensate wind to preserve inbound timing.',
                    },
                    {
                      id: 'q-rnav-ch05-1-b',
                      text: 'It avoids listening to ATC updates.',
                      isCorrect: false,
                      explanation: 'Fail: ATC communication remains mandatory during holding.',
                    },
                    {
                      id: 'q-rnav-ch05-1-c',
                      text: 'It replaces turn-direction requirements.',
                      isCorrect: false,
                      explanation: 'Fail: published turn direction is independent of timing.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch06-rnp-awareness',
              title: 'Chapter 06 - RNP Awareness',
              description: 'Monitoring containment and alerting logic in RNP operations.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch06-1',
                  text: 'RNP value primarily defines:',
                  options: [
                    {
                      id: 'q-rnav-ch06-1-a',
                      text: 'Required navigation accuracy containment for the operation.',
                      isCorrect: true,
                      explanation: 'Pass: RNP specifies required total system accuracy in operation.',
                    },
                    {
                      id: 'q-rnav-ch06-1-b',
                      text: 'Maximum turbulence tolerance only.',
                      isCorrect: false,
                      explanation: 'Fail: turbulence affects handling but is not what RNP defines.',
                    },
                    {
                      id: 'q-rnav-ch06-1-c',
                      text: 'ATC sector frequency.',
                      isCorrect: false,
                      explanation: 'Fail: frequencies are communications, not performance requirements.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch07-gnss-raim',
              title: 'Chapter 07 - GNSS RAIM Checks',
              description: 'Availability checks and contingency planning for GNSS integrity.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch07-1',
                  text: 'Why perform RAIM prediction before certain RNAV procedures?',
                  options: [
                    {
                      id: 'q-rnav-ch07-1-a',
                      text: 'To ensure integrity monitoring is likely available when needed.',
                      isCorrect: true,
                      explanation: 'Pass: integrity prediction supports reliable procedure execution.',
                    },
                    {
                      id: 'q-rnav-ch07-1-b',
                      text: 'To increase engine efficiency.',
                      isCorrect: false,
                      explanation: 'Fail: RAIM relates to navigation integrity, not propulsion efficiency.',
                    },
                    {
                      id: 'q-rnav-ch07-1-c',
                      text: 'To skip alternate planning.',
                      isCorrect: false,
                      explanation: 'Fail: alternate planning still applies under operational rules.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch08-sid-star-constraints',
              title: 'Chapter 08 - SID/STAR Constraints',
              description: 'Altitude/speed constraints and managed profile discipline.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch08-1',
                  text: 'When loading SID/STAR constraints, what is critical before execution?',
                  options: [
                    {
                      id: 'q-rnav-ch08-1-a',
                      text: 'Cross-check chart constraints with FMS entries.',
                      isCorrect: true,
                      explanation: 'Pass: chart-to-FMS cross-check catches mismatch and prevents violations.',
                    },
                    {
                      id: 'q-rnav-ch08-1-b',
                      text: 'Assume database entries always match current chart revision.',
                      isCorrect: false,
                      explanation: 'Fail: revisions may differ; verification is always needed.',
                    },
                    {
                      id: 'q-rnav-ch08-1-c',
                      text: 'Ignore speed constraints if tailwind is high.',
                      isCorrect: false,
                      explanation: 'Fail: constraints remain mandatory unless ATC amends them.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch09-map-reading',
              title: 'Chapter 09 - Enroute Chart Reading',
              description: 'Quick interpretation of airway, MORA, and reporting points.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch09-1',
                  text: 'What does MORA primarily protect against?',
                  options: [
                    {
                      id: 'q-rnav-ch09-1-a',
                      text: 'Terrain and obstacle clearance in grid segments.',
                      isCorrect: true,
                      explanation: 'Pass: MORA provides terrain/obstacle clearance reference by grid.',
                    },
                    {
                      id: 'q-rnav-ch09-1-b',
                      text: 'Radio frequency congestion.',
                      isCorrect: false,
                      explanation: 'Fail: MORA is unrelated to communications traffic load.',
                    },
                    {
                      id: 'q-rnav-ch09-1-c',
                      text: 'Fuel pricing variation.',
                      isCorrect: false,
                      explanation: 'Fail: MORA has no economic meaning.',
                    },
                  ],
                },
              ],
            },
            {
              id: 'topic-rnav-ch10-final-assessment',
              title: 'Chapter 10 - Final Assessment',
              description: 'Integrated checks across VOR, NDB, RNAV, and procedural compliance.',
              requiresSubscription: true,
              subscriptionLabel: 'Premium Access',
              questions: [
                {
                  id: 'q-rnav-ch10-1',
                  text: 'Best strategy for final mixed-navigation assessments?',
                  options: [
                    {
                      id: 'q-rnav-ch10-1-a',
                      text: 'Validate source data, cross-check instruments, and brief contingencies.',
                      isCorrect: true,
                      explanation: 'Pass: multi-layer cross-checking is key for robust navigation decisions.',
                    },
                    {
                      id: 'q-rnav-ch10-1-b',
                      text: 'Use a single source only to reduce workload.',
                      isCorrect: false,
                      explanation: 'Fail: single-source dependency increases operational risk.',
                    },
                    {
                      id: 'q-rnav-ch10-1-c',
                      text: 'Delay all checks until after takeoff.',
                      isCorrect: false,
                      explanation: 'Fail: critical checks must be completed pre-departure where required.',
                    },
                  ],
                },
              ],
            },
          ],
        },
        {
          id: 'topic-indigo-navigation',
          title: 'Indigo Navigation',
          description: 'Airline-focused SOP examples for radio navigation briefing and execution.',
          requiresSubscription: true,
          subscriptionLabel: 'Premium Access',
          questions: [
            {
              id: 'q-indigo-nav-1',
              text: 'What is the most practical SOP advantage of standardized navigation briefings?',
              options: [
                {
                  id: 'q-indigo-nav-1-a',
                  text: 'Reduces interpretation mismatch between PF and PM.',
                  isCorrect: true,
                  explanation: 'Pass: shared briefing structure improves crew coordination and consistency.',
                },
                {
                  id: 'q-indigo-nav-1-b',
                  text: 'Eliminates need for approach chart review.',
                  isCorrect: false,
                  explanation: 'Fail: chart review is always required despite SOP standardization.',
                },
                {
                  id: 'q-indigo-nav-1-c',
                  text: 'Allows bypassing route verification.',
                  isCorrect: false,
                  explanation: 'Fail: SOP cannot replace route-data verification.',
                },
              ],
            },
          ],
          videos: [
            {
              id: 'vid-indigo-nav-1',
              title: 'Indigo Navigation SOP Walkthrough',
              duration: '10:05',
              summary: 'Operational briefing flow and navigation cross-check callouts.',
            },
          ],
        },
      ],
    },
    {
      id: 'subject-meteorology',
      title: 'Meteorology',
      description: 'Weather systems, turbulence interpretation, and operational weather decision-making.',
      requiresSubscription: true,
      subscriptionLabel: 'Premium Access',
    },
    {
      id: 'subject-flight-performance',
      title: 'Flight Performance',
      description: 'Takeoff, climb, landing, and payload performance planning with scenario practice.',
      requiresSubscription: true,
      subscriptionLabel: 'Premium Access',
    },
    {
      id: 'subject-instrument-procedures',
      title: 'Instrument Procedures',
      description: 'Approach design logic, minima interpretation, and procedural compliance drills.',
      requiresSubscription: true,
      subscriptionLabel: 'Premium Access',
    },
    {
      id: 'subject-air-law',
      title: 'Air Law',
      description: 'Operational rules, controlled airspace requirements, and regulatory exam preparation.',
      requiresSubscription: true,
      subscriptionLabel: 'Premium Access',
    },
  ];

  private readonly subjects: TopicNode[] = this.ensureMinimumQuestions(this.baseSubjects);

  getSubjects(): TopicNode[] {
    return this.subjects;
  }

  getSubjectById(subjectId: string): TopicNode | undefined {
    return this.subjects.find((subject) => subject.id === subjectId);
  }

  findInitialTopic(topics: TopicNode[], fallback: TopicNode): TopicNode {
    const firstTopic = topics[0];
    if (!firstTopic) {
      return fallback;
    }
    // Keep default selection predictable: always open the first configured topic.
    return firstTopic;
  }

  private ensureMinimumQuestions(subjects: TopicNode[]): TopicNode[] {
    return subjects.map((subject) => this.ensureMinimumQuestionsInTopic(subject));
  }

  private ensureMinimumQuestionsInTopic(topic: TopicNode): TopicNode {
    const children = (topic.children || []).map((child) => this.ensureMinimumQuestionsInTopic(child));
    const questions = topic.questions ? [...topic.questions] : undefined;

    if (questions && questions.length > 0 && questions.length < 5) {
      const existingCount = questions.length;
      for (let i = existingCount + 1; i <= 5; i += 1) {
        questions.push(this.createAutoQuestion(topic.id, i));
      }
    }

    return {
      ...topic,
      children,
      questions,
    };
  }

  private createAutoQuestion(topicId: string, index: number) {
    return {
      id: `${topicId}-auto-q${index}`,
      text: `Practice check ${index}: which option best follows standard procedure?`,
      requiresSubscription: true,
      subscriptionLabel: 'Premium Access',
      options: [
        {
          id: `${topicId}-auto-q${index}-a`,
          text: 'Follow published procedure and verify with current briefing.',
          isCorrect: true,
          explanation:
            'Pass: following published guidance with briefing cross-check is the safest standard approach.',
        },
        {
          id: `${topicId}-auto-q${index}-b`,
          text: 'Skip verification if this route was flown previously.',
          isCorrect: false,
          explanation: 'Fail: previous flights do not replace current operational verification.',
        },
        {
          id: `${topicId}-auto-q${index}-c`,
          text: 'Use assumptions instead of procedure references to save time.',
          isCorrect: false,
          explanation: 'Fail: assumptions increase error risk and are not procedural practice.',
        },
      ],
    };
  }
}

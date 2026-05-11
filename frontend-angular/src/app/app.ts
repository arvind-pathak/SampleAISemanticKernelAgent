import { Component, inject, ElementRef, ViewChild, AfterViewChecked, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { timeout } from 'rxjs/operators';
import { environment } from '../environments/environment';

interface ChatMessage {
  role: 'user' | 'agent';
  content: string;
}

/**
 * Angular 21 is ZONELESS by default -- Zone.js is not included.
 * Without Zone.js, plain class properties (isLoading, messages[]) do NOT
 * trigger change detection when changed inside async callbacks like HttpClient.
 *
 * FIX: Use Angular Signals for all state the template reads.
 * signal().set() / .update() notify Angular change detection directly.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements AfterViewChecked {
  @ViewChild('chatMessages') private chatMessages!: ElementRef;

  private http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  // Signals -- reactive state that works without Zone.js
  messages = signal<ChatMessage[]>([
    {
      role: 'agent',
      content: 'Hello! I am your HR Support Agent powered by Azure OpenAI GPT-4o-mini + Semantic Kernel.\n\nTry asking me:\n\u2022 "Hi" (GreetingPlugin)\n\u2022 "How many holidays does John have?" (HRPlugin)\n\u2022 "Tell me about Sarah" (HRPlugin)\n\u2022 "What department is Mike in?" (HRPlugin)\n\nThe agent understands synonyms — try "vacation", "PTO", "time off" etc.'
    }
  ]);

  userInput = '';
  isLoading = signal(false);

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  sendMessage(): void {
    const message = this.userInput.trim();
    if (!message || this.isLoading()) return;

    this.messages.update(msgs => [...msgs, { role: 'user', content: message }]);
    this.userInput = '';
    this.isLoading.set(true);

    this.http
      .post<{ response: string }>(this.apiUrl, { message })
      .pipe(timeout(90000))
      .subscribe({
        next: (res) => {
          console.log('[Agent] Response received:', res);
          this.messages.update(msgs => [...msgs, { role: 'agent', content: res.response }]);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('[Agent] HTTP error:', err);
          const isTimeout = err?.name === 'TimeoutError';
          this.messages.update(msgs => [...msgs, {
            role: 'agent',
            content: isTimeout
              ? 'The request timed out (90s). Please try again.'
              : 'Error: Could not connect to the backend. Make sure the .NET API is running and Azure OpenAI credentials are configured in appsettings.'
          }]);
          this.isLoading.set(false);
        }
      });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    try {
      this.chatMessages.nativeElement.scrollTop = this.chatMessages.nativeElement.scrollHeight;
    } catch {}
  }
}
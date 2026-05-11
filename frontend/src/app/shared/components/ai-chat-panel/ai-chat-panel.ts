import { Component, ElementRef, ViewChild, AfterViewChecked, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiService } from '../../../core/services/ai.service';
import { MarkdownPipe } from '../../pipes/markdown.pipe';
import { LucideAngularModule, Send, X, Bot, User, Sparkles, Plus } from 'lucide-angular';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-ai-chat-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownPipe, LucideAngularModule],
  templateUrl: './ai-chat-panel.html',
  styleUrls: ['./ai-chat-panel.scss'],
  animations: [
    trigger('slideInOut', [
      state('in', style({ transform: 'translateY(0)', opacity: 1, visibility: 'visible' })),
      state('out', style({ transform: 'translateY(20px)', opacity: 0, visibility: 'hidden' })),
      transition('in => out', animate('200ms ease-in')),
      transition('out => in', animate('200ms ease-out'))
    ])
  ]
})
export class AiChatPanelComponent implements AfterViewChecked, OnDestroy {
  @ViewChild('scrollMe') private myScrollContainer!: ElementRef;

  isOpen = false;
  messageInput = '';
  
  SendIcon = Send;
  CloseIcon = X;
  BotIcon = Bot;
  UserIcon = User;
  SparklesIcon = Sparkles;
  PlusIcon = Plus;

  constructor(public aiService: AiService) {}

  ngAfterViewChecked() {
    this.scrollToBottom();
  }

  ngOnDestroy() {
    this.aiService.clearHistory();
  }

  toggleChat() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      setTimeout(() => this.scrollToBottom(), 100);
    }
  }

  sendMessage() {
    if (!this.messageInput.trim() || this.aiService.isTyping()) return;
    
    const msg = this.messageInput;
    this.messageInput = '';
    
    this.aiService.sendMessage(msg).subscribe({
      error: (err) => console.error('Failed to send AI message', err)
    });
  }

  handleKeyDown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    try {
      this.myScrollContainer.nativeElement.scrollTop = this.myScrollContainer.nativeElement.scrollHeight;
    } catch(err) { }
  }
}

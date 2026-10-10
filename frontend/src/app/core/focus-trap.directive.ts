import {
  Directive,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  OnInit,
  Output,
  inject,
} from '@angular/core';

@Directive({
  selector: '[appFocusTrap]',
  standalone: true,
})
export class FocusTrapDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef);
  private previousActiveElement: HTMLElement | null = null;

  @Input() appFocusTrapEnabled = true;
  @Output() escapeKey = new EventEmitter<void>();

  private readonly focusableSelector =
    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

  ngOnInit(): void {
    if (typeof document !== 'undefined') {
      this.previousActiveElement = document.activeElement as HTMLElement | null;
    }
    // Set initial focus
    this.focusInitialElement();
    queueMicrotask(() => this.focusInitialElement());
  }

  ngOnDestroy(): void {
    if (this.previousActiveElement && typeof this.previousActiveElement.focus === 'function') {
      this.previousActiveElement.focus();
    }
  }

  public getFocusableElements(): HTMLElement[] {
    const root = this.el.nativeElement as HTMLElement;
    return Array.from(root.querySelectorAll<HTMLElement>(this.focusableSelector)).filter(
      (node) => !node.hasAttribute('disabled') && node.getAttribute('aria-hidden') !== 'true'
    );
  }

  public focusInitialElement(): void {
    const focusables = this.getFocusableElements();
    if (focusables.length === 0) return;

    const autofocusEl = focusables.find((el) => el.hasAttribute('autofocus'));
    if (autofocusEl) {
      autofocusEl.focus();
    } else {
      focusables[0].focus();
    }
  }

  @HostListener('keydown', ['$event'])
  handleKeyDown(event: KeyboardEvent): void {
    if (!this.appFocusTrapEnabled) return;

    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      this.escapeKey.emit();
      return;
    }

    if (event.key === 'Tab') {
      const focusables = this.getFocusableElements();
      if (focusables.length === 0) {
        event.preventDefault();
        return;
      }

      const first = focusables[0];
      const last = focusables[focusables.length - 1];

      if (event.shiftKey) {
        if (document.activeElement === first || !this.el.nativeElement.contains(document.activeElement)) {
          event.preventDefault();
          last.focus();
        }
      } else {
        if (document.activeElement === last || !this.el.nativeElement.contains(document.activeElement)) {
          event.preventDefault();
          first.focus();
        }
      }
    }
  }
}

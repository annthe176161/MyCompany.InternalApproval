import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { LocalizationService, SessionStateService } from '@abp/ng.core';
import { NzI18nService, en_US, vi_VN } from 'ng-zorro-antd/i18n';
import { Subscription } from 'rxjs';
import {
  UI_EXACT_TRANSLATIONS,
  UI_PATTERN_TRANSLATIONS,
} from '../constants/ui-translations.constant';

// Dịch vụ quản lý đồng bộ ngôn ngữ giao diện toàn hệ thống (Tiếng Việt <-> Tiếng Anh)
@Injectable({
  providedIn: 'root',
})
export class AppLanguageService implements OnDestroy {
  private currentLang: 'vi' | 'en' = 'vi';
  private observer?: MutationObserver;
  private isApplying = false;
  private subscriptions: Subscription[] = [];

  // Bộ nhớ lưu văn bản tiếng Việt gốc của từng Text Node và thuộc tính phần tử DOM
  private originalTextMap = new WeakMap<Text, string>();
  private lastTranslatedTextMap = new WeakMap<Text, string>();
  private originalPlaceholderMap = new WeakMap<Element, string>();
  private lastTranslatedPlaceholderMap = new WeakMap<Element, string>();
  private originalTitleMap = new WeakMap<Element, string>();
  private lastTranslatedTitleMap = new WeakMap<Element, string>();

  constructor(
    private sessionState: SessionStateService,
    private localizationService: LocalizationService,
    private nzI18nService: NzI18nService,
    private ngZone: NgZone,
  ) {}

  // Khởi tạo lắng nghe sự kiện đổi ngôn ngữ và quan sát biến đổi DOM
  init(): void {
    const initialLang = this.sessionState.getLanguage() || this.localizationService.currentLang || 'vi';
    this.applyLanguage(initialLang);

    const sub1 = this.sessionState.getLanguage$().subscribe(lang => {
      if (lang) {
        this.applyLanguage(lang);
      }
    });

    const sub2 = this.localizationService.languageChange$.subscribe(lang => {
      if (lang) {
        this.applyLanguage(lang);
      }
    });

    this.subscriptions.push(sub1, sub2);
    this.setupDomObserver();
  }

  ngOnDestroy(): void {
    this.subscriptions.forEach(s => s.unsubscribe());
    this.observer?.disconnect();
  }

  // Áp dụng ngôn ngữ mới cho NG-ZORRO và toàn bộ cây DOM
  applyLanguage(rawLang: string): void {
    const normalized: 'vi' | 'en' = rawLang.toLowerCase().startsWith('en') ? 'en' : 'vi';
    this.currentLang = normalized;

    // Chuyển đổi locale của NG-ZORRO (Pagination 10 / trang <-> 10 / page, DatePicker, Empty, Popconfirm...)
    this.nzI18nService.setLocale(normalized === 'en' ? en_US : vi_VN);

    if (typeof document !== 'undefined' && document.body) {
      this.translateSubtree(document.body);
    }
  }

  // Thiết lập MutationObserver ngoài NgZone để dịch tức thì các modal, drawer, bảng dữ liệu mới render
  private setupDomObserver(): void {
    if (typeof document === 'undefined' || !document.body || this.observer) {
      return;
    }

    this.ngZone.runOutsideAngular(() => {
      this.observer = new MutationObserver(mutations => {
        if (this.isApplying) {
          return;
        }

        this.isApplying = true;
        try {
          for (const mutation of mutations) {
            if (mutation.type === 'childList') {
              mutation.addedNodes.forEach(node => {
                this.translateSubtree(node);
              });
            } else if (mutation.type === 'characterData' && mutation.target.nodeType === Node.TEXT_NODE) {
              const textNode = mutation.target as Text;
              const parent = textNode.parentElement;
              if (!parent || !this.shouldSkipElement(parent, true)) {
                this.processTextNode(textNode);
              }
            } else if (
              mutation.type === 'attributes' &&
              mutation.target.nodeType === Node.ELEMENT_NODE
            ) {
              const el = mutation.target as Element;
              if (!this.shouldSkipElement(el)) {
                this.processElementAttributes(el);
              }
            }
          }
        } finally {
          this.isApplying = false;
        }
      });

      this.observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true,
        attributes: true,
        attributeFilter: ['placeholder', 'title'],
      });
    });
  }

  // Duyệt và dịch toàn bộ các nút con bên trong một nhánh DOM
  private translateSubtree(root: Node): void {
    const wasApplying = this.isApplying;
    this.isApplying = true;
    try {
      if (root.nodeType === Node.TEXT_NODE) {
        const text = root as Text;
        const parent = text.parentElement;
        if (!parent || !this.shouldSkipElement(parent, true)) {
          this.processTextNode(text);
        }
        return;
      }

      if (root.nodeType === Node.ELEMENT_NODE) {
        const el = root as Element;
        if (this.shouldSkipElement(el)) {
          return;
        }
        this.processElementAttributes(el);

        const elements = el.querySelectorAll('[placeholder], [title]');
        elements.forEach(childEl => {
          if (!this.shouldSkipElement(childEl)) {
            this.processElementAttributes(childEl);
          }
        });
      }

      const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
      let currentNode = walker.nextNode();
      while (currentNode) {
        const parent = currentNode.parentElement;
        if (!parent || !this.shouldSkipElement(parent, true)) {
          this.processTextNode(currentNode as Text);
        }
        currentNode = walker.nextNode();
      }
    } finally {
      this.isApplying = wasApplying;
    }
  }

  private shouldSkipElement(el: Element, isTextNode = false): boolean {
    if (
      el.hasAttribute('data-verbatim') ||
      el.hasAttribute('data-no-translate') ||
      (el.classList && el.classList.contains('notranslate')) ||
      (typeof el.closest === 'function' && el.closest('[data-verbatim], [data-no-translate], .notranslate'))
    ) {
      return true;
    }

    const tag = el.tagName;
    if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT') {
      return true;
    }

    if (isTextNode && tag === 'TEXTAREA') {
      return true;
    }

    return false;
  }

  // Dịch hoặc khôi phục nội dung cho một Text Node
  private processTextNode(textNode: Text): void {
    const parent = textNode.parentElement;
    if (parent && this.shouldSkipElement(parent, true)) {
      return;
    }

    const currentVal = textNode.nodeValue;
    if (!currentVal || !currentVal.trim()) {
      return;
    }

    const lastTranslated = this.lastTranslatedTextMap.get(textNode);

    // Nếu nội dung của node vừa được Angular cập nhật mới (khác với bản dịch lần trước)
    if (lastTranslated !== undefined && currentVal !== lastTranslated) {
      this.originalTextMap.set(textNode, currentVal);
    } else if (!this.originalTextMap.has(textNode)) {
      this.originalTextMap.set(textNode, currentVal);
    }

    const originalText = this.originalTextMap.get(textNode) || currentVal;

    if (this.currentLang === 'vi') {
      if (textNode.nodeValue !== originalText) {
        textNode.nodeValue = originalText;
      }
      this.lastTranslatedTextMap.set(textNode, originalText);
      return;
    }

    const translated = this.translatePreservingWhitespace(originalText);
    if (textNode.nodeValue !== translated) {
      textNode.nodeValue = translated;
    }
    this.lastTranslatedTextMap.set(textNode, translated);
  }

  // Dịch hoặc khôi phục thuộc tính placeholder và title của Element
  private processElementAttributes(el: Element): void {
    if (this.shouldSkipElement(el)) {
      return;
    }

    // 1. Thuộc tính placeholder
    const placeholder = el.getAttribute('placeholder');
    if (placeholder && placeholder.trim()) {
      const lastTranslated = this.lastTranslatedPlaceholderMap.get(el);
      if (lastTranslated !== undefined && placeholder !== lastTranslated) {
        this.originalPlaceholderMap.set(el, placeholder);
      } else if (!this.originalPlaceholderMap.has(el)) {
        this.originalPlaceholderMap.set(el, placeholder);
      }
      const origPlaceholder = this.originalPlaceholderMap.get(el) || placeholder;

      if (this.currentLang === 'vi') {
        if (el.getAttribute('placeholder') !== origPlaceholder) {
          el.setAttribute('placeholder', origPlaceholder);
        }
        this.lastTranslatedPlaceholderMap.set(el, origPlaceholder);
      } else {
        const targetPlaceholder = this.translatePreservingWhitespace(origPlaceholder);
        if (el.getAttribute('placeholder') !== targetPlaceholder) {
          el.setAttribute('placeholder', targetPlaceholder);
        }
        this.lastTranslatedPlaceholderMap.set(el, targetPlaceholder);
      }
    }

    // 2. Thuộc tính title
    const title = el.getAttribute('title');
    if (title && title.trim()) {
      const lastTranslatedTitle = this.lastTranslatedTitleMap.get(el);
      if (lastTranslatedTitle !== undefined && title !== lastTranslatedTitle) {
        this.originalTitleMap.set(el, title);
      } else if (!this.originalTitleMap.has(el)) {
        this.originalTitleMap.set(el, title);
      }
      const origTitle = this.originalTitleMap.get(el) || title;

      if (this.currentLang === 'vi') {
        if (el.getAttribute('title') !== origTitle) {
          el.setAttribute('title', origTitle);
        }
        this.lastTranslatedTitleMap.set(el, origTitle);
      } else {
        const targetTitle = this.translatePreservingWhitespace(origTitle);
        if (el.getAttribute('title') !== targetTitle) {
          el.setAttribute('title', targetTitle);
        }
        this.lastTranslatedTitleMap.set(el, targetTitle);
      }
    }
  }

  // Dịch chuỗi văn bản nhưng vẫn giữ nguyên khoảng trắng đầu/cuối và các dấu phân cách (•, ·, :)
  private translatePreservingWhitespace(raw: string): string {
    const match = raw.match(/^(\s*)([\s\S]*?)(\s*)$/);
    if (!match) {
      return raw;
    }

    const [, leading, core, trailing] = match;
    if (!core) {
      return raw;
    }

    const translatedCore = this.translateCoreString(core);
    if (translatedCore === core) {
      return raw;
    }

    return `${leading}${translatedCore}${trailing}`;
  }

  // Dịch phần lõi của văn bản bằng tra cứu chính xác, tách dấu câu hoặc khớp biểu thức chính quy
  private translateCoreString(core: string): string {
    // Chuẩn hóa khoảng trắng thừa khi so khớp
    const normalized = core.replace(/\s+/g, ' ').trim();

    // 1. Tra cứu khớp chính xác 100%
    if (UI_EXACT_TRANSLATIONS[normalized]) {
      return UI_EXACT_TRANSLATIONS[normalized];
    }

    // 2. Xử lý chuỗi kết thúc bằng dấu hai chấm ":" (ví dụ: "Loại đề xuất:", "Người duyệt:")
    if (normalized.endsWith(':')) {
      const withoutColon = normalized.slice(0, -1).trim();
      if (UI_EXACT_TRANSLATIONS[withoutColon]) {
        return `${UI_EXACT_TRANSLATIONS[withoutColon]}:`;
      }
    }

    // 3. Xử lý chuỗi bắt đầu hoặc kết thúc bằng dấu "•" đơn lẻ (ví dụ: "• Đã giải ngân: 0 ₫")
    if (normalized.startsWith('•')) {
      const rest = normalized.slice(1).trim();
      const translatedRest = this.translateSingleSegment(rest);
      if (translatedRest !== rest) {
        return `• ${translatedRest}`;
      }
    }

    // 4. Xử lý chuỗi nối bằng dấu chấm tròn " • " (ví dụ: "36 tháng công tác • Mốc: Nhân sự kỳ cựu (> 3 năm)")
    if (normalized.includes('•')) {
      const parts = normalized.split(/\s*•\s*/).filter(p => p.length > 0);
      if (parts.length > 1) {
        const translatedParts = parts.map(p => this.translateSingleSegment(p));
        if (translatedParts.some((tp, idx) => tp !== parts[idx])) {
          return translatedParts.join(' • ');
        }
      }
    }

    return this.translateSingleSegment(normalized);
  }

  private translateSingleSegment(segment: string): string {
    if (UI_EXACT_TRANSLATIONS[segment]) {
      return UI_EXACT_TRANSLATIONS[segment];
    }

    for (const item of UI_PATTERN_TRANSLATIONS) {
      const match = segment.match(item.pattern);
      if (match) {
        return item.replace(...match);
      }
    }

    return segment;
  }
}

